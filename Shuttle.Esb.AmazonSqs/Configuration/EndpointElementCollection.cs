using System.Configuration;

namespace Shuttle.Esb.AmazonSqs
{
    [ConfigurationCollection(typeof(EndpointElement), AddItemName = "endpoint",
        CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class EndpointElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new EndpointElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((EndpointElement)element).Name;
        }
    }
}