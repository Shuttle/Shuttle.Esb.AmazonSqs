using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AmazonSqs
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAmazonSqs(this IServiceCollection services,
            Action<AmazonSqsConfigurationBuilder> builder = null)
        {
            Guard.AgainstNull(services, nameof(services));

            var configurationBuilder = new AmazonSqsConfigurationBuilder(services);

            builder?.Invoke(configurationBuilder);

            services.AddSingleton<IValidateOptions<AmazonSqsSettings>, AmazonSqsSettingsValidator>();

            services.TryAddSingleton<IAmazonSqsConfiguration, AmazonSqsConfiguration>();
            services.TryAddSingleton<IQueueFactory, AmazonSqsQueueFactory>();

            return services;
        }
    }
}