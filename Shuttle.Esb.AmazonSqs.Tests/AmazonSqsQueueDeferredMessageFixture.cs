using System.Threading.Tasks;
using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AmazonSqs.Tests;

public class AmazonSqsQueueDeferredMessageFixture : DeferredFixture
{
    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public async Task Should_be_able_to_perform_full_processing_async(bool isTransactionalEndpoint)
    {
        await TestDeferredProcessingAsync(AmazonSqsConfiguration.GetServiceCollection(), "amazonsqs://local/{0}", isTransactionalEndpoint);
    }
}