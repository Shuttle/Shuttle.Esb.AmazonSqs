using NUnit.Framework;
using Shuttle.Esb.Tests;
using System.Threading.Tasks;

namespace Shuttle.Esb.AmazonSqs.Tests
{
    public class AmazonSqsQueueOutboxFixture : OutboxFixture
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Should_be_able_handle_errors(bool isTransactionalEndpoint)
        {
            TestOutboxSending(AmazonSqsConfiguration.GetServiceCollection(), "amazonsqs://local/{0}", 3, isTransactionalEndpoint);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_be_able_handle_errors_async(bool isTransactionalEndpoint)
        {
            await TestOutboxSendingAsync(AmazonSqsConfiguration.GetServiceCollection(), "amazonsqs://local/{0}", 3, isTransactionalEndpoint);
        }
    }
}