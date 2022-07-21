using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AmazonSqs.Tests
{
    public class AmazonSqsQueueInboxFixture : InboxFixture
    {
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void Should_be_able_handle_errors(bool hasErrorQueue, bool isTransactionalEndpoint)
        {
            TestInboxError(AmazonSqsFixture.GetServiceCollection(), "amazonsqs://local/{0}?waitTimeSeconds=0", hasErrorQueue, isTransactionalEndpoint);
        }

        [TestCase(250, false)]
        [TestCase(250, true)]
        public void Should_be_able_to_process_messages_concurrently(int msToComplete, bool isTransactionalEndpoint)
        {
            TestInboxConcurrency(AmazonSqsFixture.GetServiceCollection(), "amazonsqs://local/{0}?waitTimeSeconds=0", msToComplete, isTransactionalEndpoint);
        }

        [TestCase(100, true)]
        [TestCase(100, false)]
        public void Should_be_able_to_process_queue_timeously(int count, bool isTransactionalEndpoint)
        {
            TestInboxThroughput(AmazonSqsFixture.GetServiceCollection(), "amazonsqs://local/{0}?waitTimeSeconds=0", 1000, count, isTransactionalEndpoint);
        }

        [Test]
        public void Should_be_able_to_handle_a_deferred_message()
        {                                           
            TestInboxDeferred(AmazonSqsFixture.GetServiceCollection(), "amazonsqs://local/{0}?waitTimeSeconds=0");
        }
    }
}