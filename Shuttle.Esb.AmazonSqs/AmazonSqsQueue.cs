using System;
using System.Collections.Generic;
using System.IO;
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

        private readonly AmazonSQSClient _client;
        private readonly object _lock = new object();
        private readonly int _maxMessages;
        private readonly string _queueName;
        private string _queueUrl;
        private bool _queueUrlResolved;
        private readonly Queue<ReceivedMessage> _receivedMessages = new Queue<ReceivedMessage>();
        private readonly List<string> _isEmptyAttributeNames = new List<string>
        {
            "ApproximateNumberOfMessages",
            "ApproximateNumberOfMessagesDelayed",
            "ApproximateNumberOfMessagesNotVisible"
        };

        public AmazonSqsQueue(Uri uri, IAmazonSqsConfiguration configuration)
        {
            Guard.AgainstNull(uri, nameof(uri));
            Guard.AgainstNull(configuration, nameof(configuration));

            Uri = uri;

            var parser = new AmazonSqsQueueUriParser(uri);

            _queueName = parser.QueueName;
            _client = new AmazonSQSClient(configuration.GetConfiguration(uri.Host));
            _maxMessages = parser.MaxMessages;
            
            GetQueueUrl();
        }

        private void GetQueueUrl()
        {
            try
            {
                _queueUrlResolved = false;

                _queueUrl = _client.GetQueueUrlAsync(new GetQueueUrlRequest
                {
                    QueueName = _queueName
                }).Result.QueueUrl;

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

        public void Create()
        {
            lock (_lock)
            {
                _client.CreateQueueAsync(new CreateQueueRequest {QueueName = _queueName}).Wait();
                
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
                foreach (var acknowledgementToken in _acknowledgementTokens.Values)
                {
                    _client.SendMessageAsync(new SendMessageRequest{QueueUrl = _queueUrl, MessageBody = acknowledgementToken.MessageBody }).Wait();
                    _client.DeleteMessageAsync(_queueUrl, acknowledgementToken.ReceiptHandle).Wait();
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

                _client.DeleteQueueAsync(new DeleteQueueRequest {QueueUrl = _queueUrl}).Wait();
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
                _client.PurgeQueueAsync(new PurgeQueueRequest {QueueUrl = _queueUrl}).Wait();
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
                var response = _client.GetQueueAttributesAsync(new GetQueueAttributesRequest
                    {
                        QueueUrl = _queueUrl,
                        AttributeNames = _isEmptyAttributeNames
                    })
                    .Result;
                return response.ApproximateNumberOfMessages == 0 && 
                       response.ApproximateNumberOfMessagesDelayed == 0 &&
                       response.ApproximateNumberOfMessagesNotVisible == 0;
            }
        }

        public void Enqueue(TransportMessage message, Stream stream)
        {
            Guard.AgainstNull(message, nameof(message));
            Guard.AgainstNull(stream, nameof(stream));
            GuardAgainstUnresolvedQueueUrl();

            _client.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = Convert.ToBase64String(stream.ToBytes())
            }).Wait();
        }

        private void GuardAgainstUnresolvedQueueUrl()
        {
            if (!_queueUrlResolved)
            {
                throw new ApplicationException(string.Format(Resources.QueueUrlNotResolvedException, _queueName));
            }
        }

        public ReceivedMessage GetMessage()
        {
            lock (_lock)
            {
                if (!_queueUrlResolved)
                {
                    return null;
                }
                
                if (_receivedMessages.Count == 0)
                {
                    var messages = _client.ReceiveMessageAsync(new ReceiveMessageRequest
                    {
                        QueueUrl = _queueUrl,
                        MaxNumberOfMessages = _maxMessages
                    }).Result;

                    foreach (var message in messages.Messages)
                    {
                        var acknowledgementToken =
                            new AcknowledgementToken(message.MessageId, message.Body, message.ReceiptHandle);

                        _acknowledgementTokens.Add(acknowledgementToken.MessageId, acknowledgementToken);

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

                _client.DeleteMessageAsync(_queueUrl, data.ReceiptHandle).Wait();
            }
        }

        public void Release(object acknowledgementToken)
        {
            GuardAgainstUnresolvedQueueUrl();
            
            Guard.AgainstNull(acknowledgementToken, nameof(acknowledgementToken));

            if (!(acknowledgementToken is AcknowledgementToken data))
            {
                return;
            }

            lock (_lock)
            {
                _client.SendMessageAsync(new SendMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MessageBody = data.MessageBody
                }).Wait();
                
                _client.DeleteMessageAsync(_queueUrl, data.ReceiptHandle).Wait();

                if (_acknowledgementTokens.ContainsKey(data.MessageId))
                {
                    _acknowledgementTokens.Remove(data.MessageId);
                }
            }
        }

        public Uri Uri { get; }

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