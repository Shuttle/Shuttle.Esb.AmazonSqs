using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AmazonSqs
{
    public class AmazonSqsBuilder
    {
        public IServiceCollection Services { get; }

        public AmazonSqsBuilder(IServiceCollection services)
        {
            Guard.AgainstNull(services, nameof(services));

            Services = services;
        }

        public AmazonSqsBuilder AddEndpoint(string name, AmazonSQSConfig configuration)
        {
            Services.AddOptions<AmazonSqsOptions>(name).Configure<IAmazonSqsConfiguration>((options, amazonSqsConfiguration) =>
            {
                options.ServiceUrl = configuration.ServiceURL;

                amazonSqsConfiguration.AddConfiguration(name, configuration);
            });

            return this;
        }

        public AmazonSqsBuilder AddEndpoint(string name, string serviceUrl)
        {
            Services.AddOptions<AmazonSqsOptions>(name).Configure(options =>
            {
                options.ServiceUrl = serviceUrl;
            });

            return this;
        }

        public AmazonSqsBuilder AddEndpoint(string name)
        {
            Services.AddOptions<AmazonSqsOptions>(name).Configure<IConfiguration, IAmazonSqsConfiguration>((option, configuration, amazonSqsConfiguration) =>
            {
                var settings = configuration.GetSection($"{AmazonSqsOptions.SectionName}").Get<AmazonSqsOptions>();

                Guard.AgainstNull(settings, nameof(settings));

                option.ServiceUrl = settings.ServiceUrl;

                amazonSqsConfiguration.AddConfiguration(name, new AmazonSQSConfig
                {
                    ServiceURL = settings.ServiceUrl
                });
            });

            return this;
        }
    }
}