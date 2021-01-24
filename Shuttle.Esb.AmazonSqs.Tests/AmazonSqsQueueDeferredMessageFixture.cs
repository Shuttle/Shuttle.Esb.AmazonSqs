using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AmazonSqs.Tests
{
    public class AmazonSqsQueueDeferredMessageFixture : DeferredFixture
    {
        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void Should_be_able_to_perform_full_processing(bool isTransactionalEndpoint)
        {
            TestDeferredProcessing(AmazonSqsFixture.GetComponentContainer(), "amazonsqs://local/{0}", isTransactionalEndpoint);
        }
    }
}