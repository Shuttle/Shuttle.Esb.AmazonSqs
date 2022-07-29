using System.Collections.Generic;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AmazonSqs
{
    public class AmazonSqsBuilder
    {
        internal readonly Dictionary<string, AmazonSqsOptions> AmazonSqsOptions = new Dictionary<string, AmazonSqsOptions>();
        public IServiceCollection Services { get; }

        public AmazonSqsBuilder(IServiceCollection services)
        {
            Guard.AgainstNull(services, nameof(services));

            Services = services;
        }

        public AmazonSqsBuilder AddOptions(string name, AmazonSqsOptions amazonSqsOptions)
        {
            Guard.AgainstNullOrEmptyString(name, nameof(name));
            Guard.AgainstNull(amazonSqsOptions, nameof(amazonSqsOptions));

            AmazonSqsOptions.Remove(name);

            AmazonSqsOptions.Add(name, amazonSqsOptions);

            return this;
        }
    }
}