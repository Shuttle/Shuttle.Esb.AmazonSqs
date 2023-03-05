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
        public async Task Should_be_able_to_distribute_messages(bool isTransactionalEndpoint)
        {
            await TestDistributor(AmazonSqsFixture.GetServiceCollection(), 
                AmazonSqsFixture.GetServiceCollection(), @"amazonsqs://local/{0}", isTransactionalEndpoint);
        }
    }
}