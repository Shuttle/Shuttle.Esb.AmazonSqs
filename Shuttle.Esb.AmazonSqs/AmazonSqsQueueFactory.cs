using System;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AmazonSqs
{
    public class AmazonSqsQueueFactory : IQueueFactory
    {
        private readonly IAmazonSqsConfiguration _configuration;
        public string Scheme => AmazonSqsQueueUriParser.Scheme;

        public AmazonSqsQueueFactory(IAmazonSqsConfiguration configuration)
        {
            Guard.AgainstNull(configuration, nameof(configuration));

            _configuration = configuration;
        }

        public IQueue Create(Uri uri)
        {
            Guard.AgainstNull(uri, "uri");

            return new AmazonSqsQueue(uri, _configuration);
        }

        public bool CanCreate(Uri uri)
        {
            Guard.AgainstNull(uri, "uri");

            return Scheme.Equals(uri.Scheme, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}