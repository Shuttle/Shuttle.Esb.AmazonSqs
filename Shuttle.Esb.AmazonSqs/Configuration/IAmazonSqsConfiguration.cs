using Amazon.SQS;

namespace Shuttle.Esb.AmazonSqs
{
    public interface IAmazonSqsConfiguration
    {
        AmazonSQSConfig GetConfiguration(string endpointName);
    }
}