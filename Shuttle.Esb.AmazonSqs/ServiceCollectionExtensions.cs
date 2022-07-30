using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AmazonSqs
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAmazonSqs(this IServiceCollection services, Action<AmazonSqsBuilder> builder = null)
        {
            Guard.AgainstNull(services, nameof(services));

            var amazonSqsBuilder = new AmazonSqsBuilder(services);

            builder?.Invoke(amazonSqsBuilder);

            services.AddSingleton<IValidateOptions<AmazonSqsOptions>, AmazonSqsOptionsValidator>();

            foreach (var pair in amazonSqsBuilder.AmazonSqsOptions)
            {
                services.AddOptions<AmazonSqsOptions>(pair.Key).Configure(options =>
                {
                    options.ServiceUrl = pair.Value.ServiceUrl;
                    options.MaxMessages = pair.Value.MaxMessages;
                    options.WaitTime = pair.Value.WaitTime;

                    if (options.MaxMessages < 1)
                    {
                        options.MaxMessages = 1;
                    }

                    if (options.MaxMessages > 32)
                    {
                        options.MaxMessages = 32;
                    }

                    if (options.WaitTime < TimeSpan.Zero)
                    {
                        options.WaitTime = TimeSpan.Zero;
                    }

                    if (options.WaitTime > TimeSpan.FromSeconds(20))
                    {
                        options.WaitTime = TimeSpan.FromSeconds(20);
                    }

                    options.Configure += (sender, args) =>
                    {
                        pair.Value.OnConfigureConsumer(sender, args);
                    };
                });
            }

            services.TryAddSingleton<IQueueFactory, AmazonSqsQueueFactory>();

            return services;
        }
    }
}