using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AmazonSqs.Tests
{
    public class AmazonSqsQueueOutboxFixture : OutboxFixture
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Should_be_able_handle_errors(bool isTransactionalEndpoint)
        {
            TestOutboxSending(AmazonSqsFixture.GetServiceCollection(), "amazonsqs://local/{0}?waitTimeSeconds=0", 3, isTransactionalEndpoint);
        }
    }
}