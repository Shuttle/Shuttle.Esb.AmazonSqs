using System;
using Shuttle.Core.Contract;
using Shuttle.Core.Threading;

namespace Shuttle.Esb.AmazonSqs
{
    public class AmazonSqsQueueFactory : IQueueFactory
    {
        private readonly IAmazonSqsConfiguration _configuration;
        private readonly ICancellationTokenSource _cancellationTokenSource;
        public string Scheme => AmazonSqsQueueUriParser.Scheme;

        public AmazonSqsQueueFactory(IAmazonSqsConfiguration configuration, ICancellationTokenSource cancellationTokenSource)
        {
            Guard.AgainstNull(configuration, nameof(configuration));
            Guard.AgainstNull(cancellationTokenSource, nameof(cancellationTokenSource));

            _configuration = configuration;
            _cancellationTokenSource = cancellationTokenSource;
        }

        public IQueue Create(Uri uri)
        {
            Guard.AgainstNull(uri, "uri");

            return new AmazonSqsQueue(uri, _configuration, _cancellationTokenSource.Get().Token);
        }
    }
}