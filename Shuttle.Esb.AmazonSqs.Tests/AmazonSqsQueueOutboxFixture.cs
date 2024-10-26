using System.Threading.Tasks;
using NUnit.Framework;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AmazonSqs.Tests;

public class AmazonSqsQueueOutboxFixture : OutboxFixture
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task Should_be_able_handle_errors_async(bool isTransactionalEndpoint)
    {
        await TestOutboxSendingAsync(AmazonSqsConfiguration.GetServiceCollection(), "amazonsqs://local/{0}", 3, isTransactionalEndpoint);
    }
}