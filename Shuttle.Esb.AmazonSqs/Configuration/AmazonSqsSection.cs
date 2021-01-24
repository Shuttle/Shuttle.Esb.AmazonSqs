using System.Configuration;
using Amazon.SQS;
using Shuttle.Core.Configuration;

namespace Shuttle.Esb.AmazonSqs
{
    public class AmazonSqsSection : ConfigurationSection
    {
        [ConfigurationProperty("endpoints", IsRequired = false, DefaultValue = null)]
        public EndpointElementCollection Endpoints => (EndpointElementCollection) this["endpoints"];

        public DefaultAmazonSqsConfiguration GetConfiguration()
        {
            var section = ConfigurationSectionProvider.Open<AmazonSqsSection>("shuttle", "amazonSqs");
            var configuration = new DefaultAmazonSqsConfiguration();

            if (section != null)
            {
                foreach (EndpointElement endpoint in section.Endpoints)
                {
                    configuration.AddConfiguration(endpoint.Name, new AmazonSQSConfig
                    {
                        ServiceURL = endpoint.ServiceUrl
                    });
                }
            }

            return configuration;
        }
    }
}