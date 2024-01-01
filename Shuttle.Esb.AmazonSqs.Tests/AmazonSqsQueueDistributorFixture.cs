using System.Threading.Tasks;
using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AmazonSqs.Tests
{
    public class AmazonSqsQueueDistributorFixture : DistributorFixture
    {
        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void Should_be_able_to_distribute_messages(bool isTransactionalEndpoint)
        {
            TestDistributor(AmazonSqsConfiguration.GetServiceCollection(), AmazonSqsConfiguration.GetServiceCollection(), @"amazonsqs://local/{0}", isTransactionalEndpoint);
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public async Task Should_be_able_to_distribute_messages_async(bool isTransactionalEndpoint)
        {
            await TestDistributorAsync(AmazonSqsConfiguration.GetServiceCollection(), AmazonSqsConfiguration.GetServiceCollection(), @"amazonsqs://local/{0}", isTransactionalEndpoint);
        }
    }
}