using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Shuttle.Esb.AmazonSqs.Tests
{
    [TestFixture]
    public class AmazonSqsSettingsFixture
    {
        protected AmazonSqsSettings GetSettings(string name)
        {
            var result = new AmazonSqsSettings();

            new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".\appsettings.json")).Build()
                .GetRequiredSection($"{AmazonSqsSettings.SectionName}:{name}").Bind(result);

            return result;
        }

        [Test]
        public void Should_be_able_to_load_a_full_configuration()
        {
            var endpointA = GetSettings("endpoint-a");

            Assert.That(endpointA.ServiceUrl.Contains("us-east-1"), Is.True);
            Assert.That(endpointA.ServiceUrl.Contains("/MyQueue"), Is.True);

            var endpointB = GetSettings("endpoint-b");

            Assert.That(endpointB.ServiceUrl.Contains("us-east-2"), Is.True);
            Assert.That(endpointB.ServiceUrl.Contains("/MyQueue"), Is.True);
        }
    }
}