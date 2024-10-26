using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shuttle.Esb.AmazonSqs.Tests;

public static class AmazonSqsConfiguration
{
    public static IServiceCollection GetServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddAmazonSqs(builder =>
        {
            var amazonSqsOptions = new AmazonSqsOptions
            {
                ServiceUrl = "http://localhost:9324",
                WaitTime = TimeSpan.FromSeconds(1),
                MaxMessages = 10
            };

            amazonSqsOptions.Configure += (sender, args) =>
            {
                Console.WriteLine($@"[event] : Configure / Uri = '{(sender as IQueue)?.Uri}'");
            };

            builder.AddOptions("local", amazonSqsOptions);
        });

        return services;
    }
}