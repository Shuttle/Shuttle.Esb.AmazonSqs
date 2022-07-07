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
                builder.AddEndpoint("local", "http://localhost:9324");
            });

            return services;
        }
    }
}