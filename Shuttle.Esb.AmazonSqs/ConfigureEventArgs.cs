using System;
using Amazon.SQS;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AmazonSqs;

public class ConfigureEventArgs
{
    private AmazonSQSConfig _amazonSqsConfig;

    public ConfigureEventArgs(AmazonSQSConfig amazonSqsConfig)
    {
        _amazonSqsConfig = Guard.AgainstNull(amazonSqsConfig);
    }

    public AmazonSQSConfig AmazonSqsConfig
    {
        get => _amazonSqsConfig;
        set => _amazonSqsConfig = value ?? throw new ArgumentNullException();
    }
}