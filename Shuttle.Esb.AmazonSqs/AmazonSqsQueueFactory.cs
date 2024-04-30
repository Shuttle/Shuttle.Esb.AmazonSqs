using System;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Threading;

namespace Shuttle.Esb.AmazonSqs
{
    public class AmazonSqsQueueFactory : IQueueFactory
    {
        private readonly IOptionsMonitor<AmazonSqsOptions> _amazonSqsOptions;
        private readonly ICancellationTokenSource _cancellationTokenSource;
        public string Scheme => "amazonsqs";

        public AmazonSqsQueueFactory(IOptionsMonitor<AmazonSqsOptions> amazonSqsOptions, ICancellationTokenSource cancellationTokenSource)
        {
            _amazonSqsOptions = Guard.AgainstNull(amazonSqsOptions, nameof(amazonSqsOptions));
            _cancellationTokenSource = Guard.AgainstNull(cancellationTokenSource, nameof(cancellationTokenSource));
        }

        public IQueue Create(Uri uri)
        {
            var queueUri = new QueueUri(Guard.AgainstNull(uri, nameof(uri))).SchemeInvariant(Scheme);
            var amazonSqsOptions = _amazonSqsOptions.Get(queueUri.ConfigurationName);

            if (amazonSqsOptions == null)
            {
                throw new InvalidOperationException(string.Format(Esb.Resources.QueueConfigurationNameException, queueUri.ConfigurationName));
            }

            return new AmazonSqsQueue(queueUri, amazonSqsOptions, _cancellationTokenSource.Get().Token);
        }
    }
}