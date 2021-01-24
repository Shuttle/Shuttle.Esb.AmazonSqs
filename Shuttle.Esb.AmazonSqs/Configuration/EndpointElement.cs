using System.Configuration;

namespace Shuttle.Esb.AmazonSqs
{
    public class EndpointElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name => (string)this["name"];

        [ConfigurationProperty("serviceUrl", IsRequired = true)]
        public string ServiceUrl => (string)this["serviceUrl"];
    }
}