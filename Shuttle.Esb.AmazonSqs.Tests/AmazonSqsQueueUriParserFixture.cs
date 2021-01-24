using System;
using NUnit.Framework;

namespace Shuttle.Esb.AmazonSqs.Tests
{
    [TestFixture]
    public class AmazonSqsQueueUriParserFixture
    {
        [Test]
        public void Should_be_able_to_parse_all_parameters()
        {
            var parser =
                new AmazonSqsQueueUriParser(
                    new Uri("amazonsqs://endpoint-name/queue-name"));

            Assert.AreEqual("endpoint-name", parser.StorageConnectionStringName);
            Assert.AreEqual("queue-name", parser.QueueName);
            Assert.AreEqual(1, parser.MaxMessages);

            parser =
                new AmazonSqsQueueUriParser(
                    new Uri("amazonsqs://endpoint-name/queue-name?maxMessages=15"));

            Assert.AreEqual("endpoint-name", parser.StorageConnectionStringName);
            Assert.AreEqual("queue-name", parser.QueueName);
            Assert.AreEqual(15, parser.MaxMessages);
        }
    }
}