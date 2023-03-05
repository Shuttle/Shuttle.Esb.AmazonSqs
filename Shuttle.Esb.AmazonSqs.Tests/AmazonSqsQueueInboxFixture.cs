using NUnit.Framework;
using Shuttle.Esb.Tests;
using System.Threading.Tasks;

namespace Shuttle.Esb.AmazonSqs.Tests
{
    public class AmazonSqsQueueInboxFixture : InboxFixture
    {
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public async Task Should_be_able_handle_errors(bool hasErrorQueue, bool isTransactionalEndpoint)
        {
            await TestInboxError(AmazonSqsFixture.GetServiceCollection(), "amazonsqs://local/{0}", hasErrorQueue, isTransactionalEndpoint);
        }

        [TestCase(250, false)]
        [TestCase(250, true)]
        public async Task Should_be_able_to_process_messages_concurrently(int msToComplete, bool isTransactionalEndpoint)
        {
            await TestInboxConcurrency(AmazonSqsFixture.GetServiceCollection(), "amazonsqs://local/{0}", msToComplete, isTransactionalEndpoint);
        }

        [TestCase(100, true)]
        [TestCase(100, false)]
        public async Task Should_be_able_to_process_queue_timeously(int count, bool isTransactionalEndpoint)
        {
            await TestInboxThroughput(AmazonSqsFixture.GetServiceCollection(), "amazonsqs://local/{0}", 1000, count, 1, isTransactionalEndpoint);
        }

        [Test]
        public async Task Should_be_able_to_handle_a_deferred_message()
        {                                           
            await TestInboxDeferred(AmazonSqsFixture.GetServiceCollection(), "amazonsqs://local/{0}");
        }
    }
}