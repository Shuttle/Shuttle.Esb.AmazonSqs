using System.Threading.Tasks;
using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AmazonSqs.Tests;

[TestFixture]
public class AmazonSqsQueueFixture : BasicQueueFixture
{
    [Test]
    public async Task Should_be_able_to_perform_simple_enqueue_and_get_message_async()
    {
        await TestSimpleEnqueueAndGetMessageAsync(AmazonSqsConfiguration.GetServiceCollection(), "amazonsqs://local/{0}");
        await TestSimpleEnqueueAndGetMessageAsync(AmazonSqsConfiguration.GetServiceCollection(), "amazonsqs://local/{0}-transient?waitTimeSeconds=0");
    }

    [Test]
    public async Task Should_be_able_to_release_a_message_async()
    {
        await TestReleaseMessageAsync(AmazonSqsConfiguration.GetServiceCollection(), "amazonsqs://local/{0}");
    }

    [Test]
    public async Task Should_be_able_to_get_message_again_when_not_acknowledged_before_queue_is_disposed_async()
    {
        await TestUnacknowledgedMessageAsync(AmazonSqsConfiguration.GetServiceCollection(), "amazonsqs://local/{0}");
    }
}