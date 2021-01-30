using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AmazonSqs.Tests
{
    [TestFixture]
    public class AmazonSqsQueueFixture : BasicQueueFixture
    {
        [Test]
        public void Should_be_able_to_perform_simple_enqueue_and_get_message()
        {
            TestSimpleEnqueueAndGetMessage(AmazonSqsFixture.GetComponentContainer(), "amazonsqs://local/{0}?waitTimeSeconds=0");
            TestSimpleEnqueueAndGetMessage(AmazonSqsFixture.GetComponentContainer(), "amazonsqs://local/{0}-transient?waitTimeSeconds=0");
        }

        [Test]
        public void Should_be_able_to_release_a_message()
        {
            TestReleaseMessage(AmazonSqsFixture.GetComponentContainer(), "amazonsqs://local/{0}?waitTimeSeconds=0");
        }

        [Test]
        public void Should_be_able_to_get_message_again_when_not_acknowledged_before_queue_is_disposed()
        {
            TestUnacknowledgedMessage(AmazonSqsFixture.GetComponentContainer(), "amazonsqs://local/{0}?waitTimeSeconds=0");
        }
    }
}