using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shuttle.Esb.AmazonSqs.Tests
{
    public static class AmazonSqsFixture
    {
        public static IServiceCollection GetServiceCollection()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddAmazonSqs(builder =>
            {
                var amazonSqsOptions = new AmazonSqsOptions
                {
                    ServiceUrl = "http://localhost:9324"
                };

                amazonSqsOptions.Configure += (sender, args) =>
                {
                    Console.WriteLine($"[event] : Configure / Uri = '{((IQueue)sender).Uri}'");
                };

                builder.AddOptions("local", amazonSqsOptions);
            });

            return services;
        }
    }
}