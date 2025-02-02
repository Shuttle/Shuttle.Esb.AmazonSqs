using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AmazonSqs;

public class AmazonSqsBuilder
{
    internal readonly Dictionary<string, AmazonSqsOptions> AmazonSqsOptions = new();

    public AmazonSqsBuilder(IServiceCollection services)
    {
        Services = Guard.AgainstNull(services);
    }

    public IServiceCollection Services { get; }

    public AmazonSqsBuilder AddOptions(string name, AmazonSqsOptions amazonSqsOptions)
    {
        Guard.AgainstNullOrEmptyString(name);
        Guard.AgainstNull(amazonSqsOptions);

        AmazonSqsOptions.Remove(name);

        AmazonSqsOptions.Add(name, amazonSqsOptions);

        return this;
    }
}