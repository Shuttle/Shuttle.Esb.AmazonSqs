using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AmazonSqs
{
    public class AmazonSqsConfigurationBuilder
    {
        private readonly IServiceCollection _services;

        public AmazonSqsConfigurationBuilder(IServiceCollection services)
        {
            Guard.AgainstNull(services, nameof(services));

            _services = services;
        }

        public AmazonSqsConfigurationBuilder AddEndpoint(string name, AmazonSQSConfig configuration)
        {
            _services.AddOptions<AmazonSqsSettings>(name).Configure<IAmazonSqsConfiguration>((options, amazonSqsConfiguration) =>
            {
                options.ServiceUrl = configuration.ServiceURL;

                amazonSqsConfiguration.AddConfiguration(name, configuration);
            });

            return this;
        }

        public AmazonSqsConfigurationBuilder AddEndpoint(string name, string serviceUrl)
        {
            _services.AddOptions<AmazonSqsSettings>(name).Configure(options =>
            {
                options.ServiceUrl = serviceUrl;
            });

            return this;
        }

        public AmazonSqsConfigurationBuilder AddEndpoint(string name)
        {
            _services.AddOptions<AmazonSqsSettings>(name).Configure<IConfiguration, IAmazonSqsConfiguration>((option, configuration, amazonSqsConfiguration) =>
            {
                var settings = configuration.GetSection($"{AmazonSqsSettings.SectionName}").Get<AmazonSqsSettings>();

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