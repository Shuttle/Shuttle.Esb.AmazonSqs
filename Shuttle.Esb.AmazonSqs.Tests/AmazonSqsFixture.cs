using Amazon.SQS;
using Moq;
using Ninject;
using Shuttle.Core.Container;
using Shuttle.Core.Ninject;
using Shuttle.Esb.Tests;

namespace Shuttle.Esb.AmazonSqs.Tests
{
    public static class AmazonSqsFixture
    {
        public static ComponentContainer GetComponentContainer()
        {
            var container = new NinjectComponentContainer(new StandardKernel());

            container.RegisterInstance(AmazonSqsConfiguration());

            return new ComponentContainer(container, () => container);
        }

        private static IAmazonSqsConfiguration AmazonSqsConfiguration()
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
