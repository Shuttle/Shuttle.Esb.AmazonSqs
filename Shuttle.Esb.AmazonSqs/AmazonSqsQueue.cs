using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Shuttle.Core.Contract;
using Shuttle.Core.Streams;

namespace Shuttle.Esb.AmazonSqs
{
    public class AmazonSqsQueue : IQueue, ICreateQueue, IDropQueue, IDisposable, IPurgeQueue
    {
        private readonly Dictionary<string, AcknowledgementToken> _acknowledgementTokens =
            new Dictionary<string, AcknowledgementToken>();

        private readonly AmazonSqsOptions _amazonSqsOptions;

        private readonly CancellationToken _cancellationToken;

        private readonly AmazonSQSClient _client;

        private readonly List<string> _isEmptyAttributeNames = new List<string>
        {
            "ApproximateNumberOfMessages",
            "ApproximateNumberOfMessagesDelayed",
            "ApproximateNumberOfMessagesNotVisible"
        };

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly TimeSpan _operationTimeout = TimeSpan.FromSeconds(30);

        private readonly Queue<ReceivedMessage> _receivedMessages = new Queue<ReceivedMessage>();
        private string _queueUrl;
        private bool _queueUrlResolved;

        public AmazonSqsQueue(QueueUri uri, AmazonSqsOptions amazonSqsOptions, CancellationToken cancellationToken)
        {
            Uri = Guard.AgainstNull(uri, nameof(uri));
            _amazonSqsOptions = Guard.AgainstNull(amazonSqsOptions, nameof(amazonSqsOptions));

            _cancellationToken = cancellationToken;

            var amazonSqsConfig = new AmazonSQSConfig
            {
                ServiceURL = amazonSqsOptions.ServiceUrl
            };

            _amazonSqsOptions.OnConfigureConsumer(this, new ConfigureEventArgs(amazonSqsConfig));

            _client = new AmazonSQSClient(amazonSqsConfig);

            GetQueueUrl().GetAwaiter().GetResult();
        }

        public async Task Create()
        {
            await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                await _client.CreateQueueAsync(new CreateQueueRequest { QueueName = Uri.QueueName }, _cancellationToken).ConfigureAwait(false);

                OperationCompleted.Invoke(this, new OperationCompletedEventArgs("Create"));

                await GetQueueUrl();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _lock.Release();
            }
        }

        public void Dispose()
        {
            if (!_queueUrlResolved)
            {
                return;
            }

            _lock.Wait(CancellationToken.None);

            try
            {
                // Cannot use _cancellationToken since stopping the service bus will cancel it.
                foreach (var acknowledgementToken in _acknowledgementTokens.Values)
                {
                    try
                    {
                        _client.SendMessageAsync(
                            new SendMessageRequest
                                { QueueUrl = _queueUrl, MessageBody = acknowledgementToken.MessageBody }
                        ).Wait(_operationTimeout);
                        _client.DeleteMessageAsync(_queueUrl, acknowledgementToken.ReceiptHandle)
                            .Wait(_operationTimeout);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                _acknowledgementTokens.Clear();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task Drop()
        {
            await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            if (!_queueUrlResolved)
            {
                return;
            }

            try
            {
                await _client.DeleteQueueAsync(new DeleteQueueRequest { QueueUrl = _queueUrl }, _cancellationToken).ConfigureAwait(false);

                OperationCompleted.Invoke(this, new OperationCompletedEventArgs("Drop"));
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task Purge()
        {
            if (!_queueUrlResolved)
            {
                return;
            }

            await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                await _client.PurgeQueueAsync(new PurgeQueueRequest { QueueUrl = _queueUrl }, _cancellationToken).ConfigureAwait(false);

                OperationCompleted.Invoke(this, new OperationCompletedEventArgs("Purge"));
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<bool> IsEmpty()
        {
            if (!_queueUrlResolved)
            {
                OperationCompleted.Invoke(this, new OperationCompletedEventArgs("IsEmpty", true));

                return true;
            }

            await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                var response = _client.GetQueueAttributesAsync(new GetQueueAttributesRequest
                {
                    QueueUrl = _queueUrl,
                    AttributeNames = _isEmptyAttributeNames
                }, _cancellationToken).Result;

                var result = response.ApproximateNumberOfMessages == 0 &&
                             response.ApproximateNumberOfMessagesDelayed == 0 &&
                             response.ApproximateNumberOfMessagesNotVisible == 0;

                OperationCompleted.Invoke(this, new OperationCompletedEventArgs("IsEmpty", result));

                return result;
            }
            catch (OperationCanceledException)
            {
                return true;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task Enqueue(TransportMessage message, Stream stream)
        {
            Guard.AgainstNull(message, nameof(message));
            Guard.AgainstNull(stream, nameof(stream));

            GuardAgainstUnresolvedQueueUrl();

            await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                await _client.SendMessageAsync(new SendMessageRequest { QueueUrl = _queueUrl, MessageBody = Convert.ToBase64String(await stream.ToBytesAsync()) }, _cancellationToken).ConfigureAwait(false);

                MessageEnqueued.Invoke(this, new MessageEnqueuedEventArgs(message, stream));
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<ReceivedMessage> GetMessage()
        {
            if (!_queueUrlResolved)
            {
                return null;
            }

            await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                if (_receivedMessages.Count == 0)
                {
                    ReceiveMessageResponse messages;

                    try
                    {
                        messages = await _client.ReceiveMessageAsync(new ReceiveMessageRequest
                        {
                            QueueUrl = _queueUrl,
                            MaxNumberOfMessages = _amazonSqsOptions.MaxMessages,
                            WaitTimeSeconds = (int)_amazonSqsOptions.WaitTime.TotalSeconds
                        }, _cancellationToken).ConfigureAwait(false);
                    }
                    catch
                    {
                        return null;
                    }

                    foreach (var message in messages.Messages)
                    {
                        var acknowledgementToken =
                            new AcknowledgementToken(message.MessageId, message.Body, message.ReceiptHandle);

                        if (!_acknowledgementTokens.ContainsKey(acknowledgementToken.MessageId))
                        {
                            _acknowledgementTokens.Add(acknowledgementToken.MessageId, acknowledgementToken);
                        }

                        _receivedMessages.Enqueue(new ReceivedMessage(
                            new MemoryStream(Convert.FromBase64String(message.Body)),
                            acknowledgementToken));
                    }
                }

                var receivedMessage = _receivedMessages.Count > 0 ? _receivedMessages.Dequeue() : null;

                if (receivedMessage != null)
                {
                    MessageReceived.Invoke(this, new MessageReceivedEventArgs(receivedMessage));
                }

                return receivedMessage;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task Acknowledge(object acknowledgementToken)
        {
            Guard.AgainstNull(acknowledgementToken, nameof(acknowledgementToken));

            GuardAgainstUnresolvedQueueUrl();

            await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                if (!(acknowledgementToken is AcknowledgementToken data))
                {
                    return;
                }

                if (_acknowledgementTokens.ContainsKey(data.MessageId))
                {
                    _acknowledgementTokens.Remove(data.MessageId);
                }

                try
                {
                    await _client.DeleteMessageAsync(_queueUrl, data.ReceiptHandle, _cancellationToken).ConfigureAwait(false);

                    MessageAcknowledged.Invoke(this, new MessageAcknowledgedEventArgs(acknowledgementToken));
                }
                catch (OperationCanceledException)
                {
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task Release(object acknowledgementToken)
        {
            Guard.AgainstNull(acknowledgementToken, nameof(acknowledgementToken));

            GuardAgainstUnresolvedQueueUrl();

            if (!(acknowledgementToken is AcknowledgementToken data))
            {
                return;
            }

            await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                await _client.SendMessageAsync(new SendMessageRequest { QueueUrl = _queueUrl, MessageBody = data.MessageBody }, _cancellationToken).ConfigureAwait(false);
                await _client.DeleteMessageAsync(_queueUrl, data.ReceiptHandle, _cancellationToken).ConfigureAwait(false);

                MessageReleased.Invoke(this, new MessageReleasedEventArgs(acknowledgementToken));
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _lock.Release();
            }

            if (_acknowledgementTokens.ContainsKey(data.MessageId))
            {
                _acknowledgementTokens.Remove(data.MessageId);
            }
        }

        public QueueUri Uri { get; }
        public bool IsStream => false;

        public event EventHandler<MessageEnqueuedEventArgs> MessageEnqueued = delegate
        {
        };

        public event EventHandler<MessageAcknowledgedEventArgs> MessageAcknowledged = delegate
        {
        };

        public event EventHandler<MessageReleasedEventArgs> MessageReleased = delegate
        {
        };

        public event EventHandler<MessageReceivedEventArgs> MessageReceived = delegate
        {
        };

        public event EventHandler<OperationCompletedEventArgs> OperationCompleted = delegate
        {
        };

        private async Task GetQueueUrl()
        {
            try
            {
                _queueUrlResolved = false;

                try
                {
                    _queueUrl = (await _client.GetQueueUrlAsync(new GetQueueUrlRequest { QueueName = Uri.QueueName }, _cancellationToken).ConfigureAwait(false)).QueueUrl;
                }
                catch (OperationCanceledException)
                {
                }
                catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
                {
                }

                _queueUrlResolved = !string.IsNullOrWhiteSpace(_queueUrl);
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("NonExistentQueue"))
                {
                    throw;
                }
            }
        }

        private void GuardAgainstUnresolvedQueueUrl()
        {
            if (!_queueUrlResolved)
            {
                throw new ApplicationException(string.Format(Resources.QueueUrlNotResolvedException, Uri.QueueName));
            }
        }

        internal class AcknowledgementToken
        {
            public AcknowledgementToken(string messageId, string messageBody, string receiptHandle)
            {
                MessageId = messageId;
                MessageBody = messageBody;
                ReceiptHandle = receiptHandle;
            }

            public string MessageBody { get; }

            public string MessageId { get; }
            public string ReceiptHandle { get; }
        }
    }
}