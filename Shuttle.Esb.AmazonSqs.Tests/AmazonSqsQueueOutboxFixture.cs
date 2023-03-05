using NUnit.Framework;
using Shuttle.Esb.Tests;
using System.Threading.Tasks;

namespace Shuttle.Esb.AmazonSqs.Tests
{
    public class AmazonSqsQueueOutboxFixture : OutboxFixture
    {
        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_be_able_handle_errors(bool isTransactionalEndpoint)
        {
            await TestOutboxSending(AmazonSqsFixture.GetServiceCollection(), "amazonsqs://local/{0}", 3, isTransactionalEndpoint);
        }
    }
}