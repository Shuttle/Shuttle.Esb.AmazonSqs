using Amazon.SQS;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AmazonSqs
{
    public class ConfigureEventArgs
    {
        private AmazonSQSConfig _amazonSqsConfig;

        public AmazonSQSConfig AmazonSqsConfig
        {
            get => _amazonSqsConfig;
            set => _amazonSqsConfig = value ?? throw new System.ArgumentNullException();
        }

        public ConfigureEventArgs(AmazonSQSConfig amazonSqsConfig)
        {
            Guard.AgainstNull(amazonSqsConfig, nameof(amazonSqsConfig));

            _amazonSqsConfig = amazonSqsConfig;
        }
    }
}