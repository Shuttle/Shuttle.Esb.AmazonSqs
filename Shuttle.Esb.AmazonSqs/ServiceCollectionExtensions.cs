using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AmazonSqs
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAmazonSqs(this IServiceCollection services,
            Action<AmazonSqsBuilder> builder = null)
        {
            Guard.AgainstNull(services, nameof(services));

            var configurationBuilder = new AmazonSqsBuilder(services);

            builder?.Invoke(configurationBuilder);

            services.AddSingleton<IValidateOptions<AmazonSqsOptions>, AmazonSqsOptionsValidator>();

            services.TryAddSingleton<IAmazonSqsConfiguration, AmazonSqsConfiguration>();
            services.TryAddSingleton<IQueueFactory, AmazonSqsQueueFactory>();

            return services;
        }
    }
}