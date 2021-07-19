using System;
using System.IO;
using System.Linq;
using System.Net;
using NUnit.Framework;
using Shuttle.Core.Configuration;

namespace Shuttle.Esb.AmazonSqs.Tests
{
    [TestFixture]
    public class AmazonSqsSectionFixture
    {
        protected AmazonSqsSection GetAmazonSqsSection(string file)
        {
            return ConfigurationSectionProvider.OpenFile<AmazonSqsSection>("shuttle", "amazonsqs",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@".\files\{file}"));
        }

        [Test]
        [TestCase("AmazonSqs.config")]
        [TestCase("AmazonSqs-Grouped.config")]
        public void Should_be_able_to_load_a_full_configuration(string file)
        {
            var section = GetAmazonSqsSection(file);

            Assert.IsNotNull(section);

            Assert.AreEqual(2, section.Endpoints.Count);

            foreach (EndpointElement endpoint in section.Endpoints)
            {
                Assert.That(endpoint.ServiceUrl.Contains("/MyQueue"), Is.True);
            }
        }
    }
}