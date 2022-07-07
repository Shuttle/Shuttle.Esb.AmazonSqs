using System;
using System.Collections.Generic;
using Amazon.SQS;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AmazonSqs
{
    public class AmazonSqsConfiguration : IAmazonSqsConfiguration
    {
        private readonly IOptionsMonitor<AmazonSqsSettings> _amazonSqsOptions;

        private readonly Dictionary<string, AmazonSQSConfig>
            _configurations = new Dictionary<string, AmazonSQSConfig>();

        public AmazonSqsConfiguration(IOptionsMonitor<AmazonSqsSettings> amazonSqsOptions)
        {
            Guard.AgainstNull(amazonSqsOptions, nameof(amazonSqsOptions));

            _amazonSqsOptions = amazonSqsOptions;
        }

        public AmazonSQSConfig GetConfiguration(string endpointName)
        {
            Guard.AgainstNullOrEmptyString(endpointName, nameof(endpointName));

            if (!_configurations.ContainsKey(endpointName))
            {
                var amazonSqsSettings = _amazonSqsOptions.Get(endpointName);

                if (amazonSqsSettings != null)
                {
                    _configurations.Add(endpointName, new AmazonSQSConfig
                    {
                        ServiceURL = amazonSqsSettings.ServiceUrl
                    });
                }
                else
                {
                    throw new InvalidOperationException(string.Format(Resources.EndpointConfigurationMissingException,
                        endpointName));
                }
            }

            return _configurations[endpointName];
        }

        public void AddConfiguration(string endpointName, AmazonSQSConfig configuration)
        {
            Guard.AgainstNullOrEmptyString(endpointName,nameof(endpointName));
            Guard.AgainstNull(configuration, nameof(configuration));

            if (_configurations.ContainsKey(endpointName))
            {
                throw new InvalidOperationException(string.Format(Resources.DuplicateEndpointConfigurationException,
                    endpointName));
            }

            _configurations.Add(endpointName, configuration);
        }
    }
}