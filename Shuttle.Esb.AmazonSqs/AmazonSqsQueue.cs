using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Shuttle.Core.Contract;
using Shuttle.Core.Streams;

namespace Shuttle.Esb.AmazonSqs;

public class AmazonSqsQueue : IQueue, ICreateQueue, IDropQueue, IPurgeQueue, IDisposable
{
    private readonly Dictionary<string, AcknowledgementToken> _acknowledgementTokens = new();

    private readonly AmazonSqsOptions _amazonSqsOptions;

    private readonly CancellationToken _cancellationToken;

    private readonly AmazonSQSClient _client;

    private readonly List<string> _isEmptyAttributeNames =
    [
        "ApproximateNumberOfMessages",
        "ApproximateNumberOfMessagesDelayed",
        "ApproximateNumberOfMessagesNotVisible"
    ];

    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly TimeSpan _operationTimeout = TimeSpan.FromSeconds(30);

    private readonly Queue<ReceivedMessage> _receivedMessages = new();
    private string _queueUrl = string.Empty;
    private bool _queueUrlResolved;

    public AmazonSqsQueue(QueueUri uri, AmazonSqsOptions amazonSqsOptions, CancellationToken cancellationToken)
    {
        Uri = Guard.AgainstNull(uri);
        _amazonSqsOptions = Guard.AgainstNull(amazonSqsOptions);

        _cancellationToken = cancellationToken;

        var amazonSqsConfig = new AmazonSQSConfig
        {
            ServiceURL = amazonSqsOptions.ServiceUrl
        };

        _amazonSqsOptions.OnConfigureConsumer(this, new(amazonSqsConfig));

        _client = new(amazonSqsConfig);

        GetQueueUrl().GetAwaiter().GetResult();
    }

    public async Task CreateAsync()
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            Operation?.Invoke(this, new("[create/cancelled]"));
            return;
        }

        Operation?.Invoke(this, new("[create/starting]"));

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await _client.CreateQueueAsync(new CreateQueueRequest { QueueName = Uri.QueueName }, _cancellationToken).ConfigureAwait(false);
            await GetQueueUrl();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _lock.Release();
        }

        Operation?.Invoke(this, new("[create/completed]"));
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
                    _client.SendMessageAsync(new() { QueueUrl = _queueUrl, MessageBody = acknowledgementToken.MessageBody }).Wait(_operationTimeout);
                    _client.DeleteMessageAsync(_queueUrl, acknowledgementToken.ReceiptHandle).Wait(_operationTimeout);
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

    public async Task DropAsync()
    {
        if (!_queueUrlResolved)
        {
            return;
        }

        if (_cancellationToken.IsCancellationRequested)
        {
            Operation?.Invoke(this, new("[drop/cancelled]"));
            return;
        }

        Operation?.Invoke(this, new("[drop/starting]"));

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await _client.DeleteQueueAsync(new DeleteQueueRequest { QueueUrl = _queueUrl }, _cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Operation?.Invoke(this, new("[drop/cancelled]"));
        }
        finally
        {
            _lock.Release();
        }

        Operation?.Invoke(this, new("[drop/completed]"));
    }

    public async Task PurgeAsync()
    {
        if (!_queueUrlResolved)
        {
            return;
        }

        if (_cancellationToken.IsCancellationRequested)
        {
            Operation?.Invoke(this, new("[purge/cancelled]"));
            return;
        }

        Operation?.Invoke(this, new("[purge/starting]"));

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await _client.PurgeQueueAsync(new PurgeQueueRequest { QueueUrl = _queueUrl }, _cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Operation?.Invoke(this, new("[purge/cancelled]"));
        }
        finally
        {
            _lock.Release();
        }

        Operation?.Invoke(this, new("[purge/completed]"));
    }

    public async ValueTask<bool> IsEmptyAsync()
    {
        if (!_queueUrlResolved)
        {
            Operation?.Invoke(this, new("[is-empty]", true));

            return true;
        }

        if (_cancellationToken.IsCancellationRequested)
        {
            Operation?.Invoke(this, new("[is-empty/cancelled]", true));
            return true;
        }

        Operation?.Invoke(this, new("[is-empty/starting]"));

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            var response = _client.GetQueueAttributesAsync(new()
            {
                QueueUrl = _queueUrl,
                AttributeNames = _isEmptyAttributeNames
            }, _cancellationToken).Result;

            var result =
                response.ApproximateNumberOfMessages == 0 &&
                response is { ApproximateNumberOfMessagesDelayed: 0, ApproximateNumberOfMessagesNotVisible: 0 };

            Operation?.Invoke(this, new("[is-empty]", result));

            return result;
        }
        catch (OperationCanceledException)
        {
            Operation?.Invoke(this, new("[is-empty/cancelled]", true));
        }
        finally
        {
            _lock.Release();
        }

        return true;
    }

    public async Task EnqueueAsync(TransportMessage message, Stream stream)
    {
        Guard.AgainstNull(message);
        Guard.AgainstNull(stream);

        GuardAgainstUnresolvedQueueUrl();

        if (_cancellationToken.IsCancellationRequested)
        {
            Operation?.Invoke(this, new("[enqueue/cancelled]"));
            return;
        }

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await _client.SendMessageAsync(new() { QueueUrl = _queueUrl, MessageBody = Convert.ToBase64String(await stream.ToBytesAsync()) }, _cancellationToken).ConfigureAwait(false);

            MessageEnqueued?.Invoke(this, new(message, stream));
        }
        catch (OperationCanceledException)
        {
            Operation?.Invoke(this, new("[enqueue/cancelled]"));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ReceivedMessage?> GetMessageAsync()
    {
        if (!_queueUrlResolved)
        {
            return null;
        }

        if (_cancellationToken.IsCancellationRequested)
        {
            Operation?.Invoke(this, new("[get-message/cancelled]"));
            return null;
        }

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (_receivedMessages.Count == 0)
            {
                var messages = await _client.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = _amazonSqsOptions.MaxMessages,
                    WaitTimeSeconds = (int)_amazonSqsOptions.WaitTime.TotalSeconds
                }, _cancellationToken).ConfigureAwait(false);

                foreach (var message in messages.Messages)
                {
                    var acknowledgementToken = new AcknowledgementToken(message.MessageId, message.Body, message.ReceiptHandle);

                    _acknowledgementTokens.TryAdd(acknowledgementToken.MessageId, acknowledgementToken);

                    _receivedMessages.Enqueue(new(new MemoryStream(Convert.FromBase64String(message.Body)), acknowledgementToken));
                }
            }

            var receivedMessage = _receivedMessages.Count > 0 ? _receivedMessages.Dequeue() : null;

            if (receivedMessage != null)
            {
                MessageReceived?.Invoke(this, new(receivedMessage));
            }

            return receivedMessage;
        }
        catch (OperationCanceledException)
        {
            Operation?.Invoke(this, new("[get-message/cancelled]"));
        }
        finally
        {
            _lock.Release();
        }

        return null;
    }

    public async Task AcknowledgeAsync(object acknowledgementToken)
    {
        Guard.AgainstNull(acknowledgementToken);

        GuardAgainstUnresolvedQueueUrl();

        if (_cancellationToken.IsCancellationRequested)
        {
            Operation?.Invoke(this, new("[acknowledge/cancelled]"));
            return;
        }

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

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

            MessageAcknowledged?.Invoke(this, new(acknowledgementToken));
        }
        catch (OperationCanceledException)
        {
            Operation?.Invoke(this, new("[acknowledge/cancelled]"));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ReleaseAsync(object acknowledgementToken)
    {
        Guard.AgainstNull(acknowledgementToken);

        GuardAgainstUnresolvedQueueUrl();

        if (!(acknowledgementToken is AcknowledgementToken data))
        {
            return;
        }

        if (_cancellationToken.IsCancellationRequested)
        {
            Operation?.Invoke(this, new("[release/cancelled]"));
            return;
        }

        await _lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            await _client.SendMessageAsync(new() { QueueUrl = _queueUrl, MessageBody = data.MessageBody }, _cancellationToken).ConfigureAwait(false);
            await _client.DeleteMessageAsync(_queueUrl, data.ReceiptHandle, _cancellationToken).ConfigureAwait(false);

            MessageReleased?.Invoke(this, new(acknowledgementToken));
        }
        catch (OperationCanceledException)
        {
            Operation?.Invoke(this, new("[release/cancelled]"));
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

    public event EventHandler<MessageEnqueuedEventArgs>? MessageEnqueued;
    public event EventHandler<MessageAcknowledgedEventArgs>? MessageAcknowledged;
    public event EventHandler<MessageReleasedEventArgs>? MessageReleased;
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
    public event EventHandler<OperationEventArgs>? Operation;

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
        catch (AmazonSQSException ex)
        {
            if (!ex.ErrorCode.Equals("QueueDoesNotExist", StringComparison.InvariantCulture) &&
                !ex.ErrorCode.Equals("AWS.SimpleQueueService.NonExistentQueue", StringComparison.InvariantCulture))
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