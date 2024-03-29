﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Streams;

namespace Shuttle.Esb.AmazonSqs
{
    public class AmazonSqsQueue : IQueue, ICreateQueue, IDropQueue, IDisposable, IPurgeQueue
    {
        public static readonly string Scheme = "amazonsqs";

        private readonly Dictionary<string, AcknowledgementToken> _acknowledgementTokens =
            new Dictionary<string, AcknowledgementToken>();

        private readonly CancellationToken _cancellationToken;

        private readonly AmazonSQSClient _client;

        private readonly List<string> _isEmptyAttributeNames = new List<string>
        {
            "ApproximateNumberOfMessages",
            "ApproximateNumberOfMessagesDelayed",
            "ApproximateNumberOfMessagesNotVisible"
        };

        private readonly object _lock = new object();
        private readonly Queue<ReceivedMessage> _receivedMessages = new Queue<ReceivedMessage>();
        private string _queueUrl;
        private bool _queueUrlResolved;
        private readonly TimeSpan _operationTimeout = TimeSpan.FromSeconds(30);
        private readonly AmazonSqsOptions _amazonSqsOptions;

        public AmazonSqsQueue(QueueUri uri, AmazonSqsOptions amazonSqsOptions, CancellationToken cancellationToken)
        {
            Guard.AgainstNull(uri, nameof(uri));
            Guard.AgainstNull(amazonSqsOptions, nameof(amazonSqsOptions));

            _cancellationToken = cancellationToken;

            Uri = uri;

            _amazonSqsOptions = amazonSqsOptions;

            var amazonSqsConfig = new AmazonSQSConfig
            {
                ServiceURL = amazonSqsOptions.ServiceUrl
            };

            _amazonSqsOptions.OnConfigureConsumer(this, new ConfigureEventArgs(amazonSqsConfig));

            _client = new AmazonSQSClient(amazonSqsConfig);

            GetQueueUrl();
        }

        public void Create()
        {
            lock (_lock)
            {
                try
                {
                    _client.CreateQueueAsync(new CreateQueueRequest { QueueName = Uri.QueueName }, _cancellationToken)
                        .Wait(_cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }

                GetQueueUrl();
            }
        }

        public void Dispose()
        {
            if (!_queueUrlResolved)
            {
                return;
            }

            lock (_lock)
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
        }

        public void Drop()
        {
            lock (_lock)
            {
                if (!_queueUrlResolved)
                {
                    return;
                }

                try
                {
                    _client.DeleteQueueAsync(new DeleteQueueRequest { QueueUrl = _queueUrl }, _cancellationToken).Wait(_cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        public void Purge()
        {
            if (!_queueUrlResolved)
            {
                return;
            }

            lock (_lock)
            {
                try
                {
                    _client.PurgeQueueAsync(new PurgeQueueRequest { QueueUrl = _queueUrl }, _cancellationToken).Wait(_cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        public bool IsEmpty()
        {
            if (!_queueUrlResolved)
            {
                return true;
            }

            lock (_lock)
            {
                try
                {
                    var response = _client.GetQueueAttributesAsync(new GetQueueAttributesRequest
                    {
                        QueueUrl = _queueUrl,
                        AttributeNames = _isEmptyAttributeNames
                    }, _cancellationToken).Result;

                    return response.ApproximateNumberOfMessages == 0 &&
                           response.ApproximateNumberOfMessagesDelayed == 0 &&
                           response.ApproximateNumberOfMessagesNotVisible == 0;
                }
                catch (OperationCanceledException)
                {
                    return true;
                }
            }
        }

        public void Enqueue(TransportMessage message, Stream stream)
        {
            Guard.AgainstNull(message, nameof(message));
            Guard.AgainstNull(stream, nameof(stream));
           
            GuardAgainstUnresolvedQueueUrl();

            lock (_lock)
            {
                try
                {
                    _client.SendMessageAsync(new SendMessageRequest
                    {
                        QueueUrl = _queueUrl,
                        MessageBody = Convert.ToBase64String(stream.ToBytes())
                    }, _cancellationToken).Wait(_cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        public ReceivedMessage GetMessage()
        {
            if (!_queueUrlResolved)
            {
                return null;
            }

            lock (_lock)
            {
                if (_receivedMessages.Count == 0)
                {
                    ReceiveMessageResponse messages;

                    try
                    {
                        messages = _client.ReceiveMessageAsync(new ReceiveMessageRequest
                        {
                            QueueUrl = _queueUrl,
                            MaxNumberOfMessages = _amazonSqsOptions.MaxMessages,
                            WaitTimeSeconds = (int)_amazonSqsOptions.WaitTime.TotalSeconds
                        }, _cancellationToken).Result;
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

                return _receivedMessages.Count > 0 ? _receivedMessages.Dequeue() : null;
            }
        }

        public void Acknowledge(object acknowledgementToken)
        {
            Guard.AgainstNull(acknowledgementToken, nameof(acknowledgementToken));
            
            GuardAgainstUnresolvedQueueUrl();

            lock (_lock)
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
                    _client.DeleteMessageAsync(_queueUrl, data.ReceiptHandle, _cancellationToken).Wait(_cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        public void Release(object acknowledgementToken)
        {
            Guard.AgainstNull(acknowledgementToken, nameof(acknowledgementToken));

            GuardAgainstUnresolvedQueueUrl();

            if (!(acknowledgementToken is AcknowledgementToken data))
            {
                return;
            }

            lock (_lock)
            {
                try
                {
                    _client.SendMessageAsync(new SendMessageRequest
                    {
                        QueueUrl = _queueUrl,
                        MessageBody = data.MessageBody
                    }, _cancellationToken).Wait(_cancellationToken);

                    _client.DeleteMessageAsync(_queueUrl, data.ReceiptHandle, _cancellationToken).Wait(_cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }

                if (_acknowledgementTokens.ContainsKey(data.MessageId))
                {
                    _acknowledgementTokens.Remove(data.MessageId);
                }
            }
        }

        public QueueUri Uri { get; }
        public bool IsStream => false;

        private void GetQueueUrl()
        {
            try
            {
                _queueUrlResolved = false;

                try
                {
                    _queueUrl = _client.GetQueueUrlAsync(new GetQueueUrlRequest
                    {
                        QueueName = Uri.QueueName
                    }, _cancellationToken).Result.QueueUrl;
                }
                catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
                {
                }

                _queueUrlResolved = !string.IsNullOrWhiteSpace(_queueUrl);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null &&
                    !ex.InnerException.Message.Contains("NonExistentQueue"))
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

            public string MessageId { get; }
            public string MessageBody { get; }
            public string ReceiptHandle { get; }
        }
    }
}