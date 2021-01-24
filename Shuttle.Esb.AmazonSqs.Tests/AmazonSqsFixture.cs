using Amazon.SQS;
using Moq;
using Ninject;
using Shuttle.Core.Container;
using Shuttle.Core.Ninject;

namespace Shuttle.Esb.AmazonSqs.Tests
{
    public static class AmazonSqsFixture
    {
        public static Esb.Tests.ComponentContainer GetComponentContainer()
        {
            var container = new NinjectComponentContainer(new StandardKernel());

            container.RegisterInstance(AzureStorageConfiguration());

            return new Esb.Tests.ComponentContainer(container, () => container);
        }

        private static IAmazonSqsConfiguration AzureStorageConfiguration()
        {
            var mock = new Mock<IAmazonSqsConfiguration>();

            mock.Setup(m => m.GetConfiguration(It.IsAny<string>())).Returns(new AmazonSQSConfig
            {
                ServiceURL = "http://localhost:9324"
            });

            return mock.Object;
        }
    }
}
