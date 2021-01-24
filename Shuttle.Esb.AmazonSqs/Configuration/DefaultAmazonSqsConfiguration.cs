using System.Collections.Generic;
using System.Configuration;
using Amazon.SQS;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AmazonSqs
{
    public class DefaultAmazonSqsConfiguration : IAmazonSqsConfiguration
    {
        private readonly Dictionary<string, AmazonSQSConfig>
            _configurations = new Dictionary<string, AmazonSQSConfig>();
        
        public AmazonSQSConfig GetConfiguration(string endpointName)
        {
            Guard.AgainstNullOrEmptyString(endpointName, nameof(endpointName));

            if (_configurations.ContainsKey(endpointName))
            {
                throw new ConfigurationErrorsException(string.Format(Resources.EndpointConfigurationMissingException,
                    endpointName));
            }

            return _configurations[endpointName];
        }

        public void AddConfiguration(string endpointName, AmazonSQSConfig configuration)
        {
            Guard.AgainstNullOrEmptyString(endpointName,nameof(endpointName));
            Guard.AgainstNull(configuration, nameof(configuration));

            if (_configurations.ContainsKey(endpointName))
            {
                throw new ConfigurationErrorsException(string.Format(Resources.DuplicateEndpointConfigurationException,
                    endpointName));
            }

            _configurations.Add(endpointName, configuration);
        }
    }
}