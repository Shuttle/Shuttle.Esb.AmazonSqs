using System;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Threading;

namespace Shuttle.Esb.AmazonSqs;

public class AmazonSqsQueueFactory : IQueueFactory
{
    private readonly IOptionsMonitor<AmazonSqsOptions> _amazonSqsOptions;
    private readonly ICancellationTokenSource _cancellationTokenSource;

    public AmazonSqsQueueFactory(IOptionsMonitor<AmazonSqsOptions> amazonSqsOptions, ICancellationTokenSource cancellationTokenSource)
    {
        _amazonSqsOptions = Guard.AgainstNull(amazonSqsOptions);
        _cancellationTokenSource = Guard.AgainstNull(cancellationTokenSource);
    }

    public string Scheme => "amazonsqs";

    public IQueue Create(Uri uri)
    {
        var queueUri = new QueueUri(Guard.AgainstNull(uri)).SchemeInvariant(Scheme);
        var amazonSqsOptions = _amazonSqsOptions.Get(queueUri.ConfigurationName);

        if (amazonSqsOptions == null)
        {
            throw new InvalidOperationException(string.Format(Esb.Resources.QueueConfigurationNameException, queueUri.ConfigurationName));
        }

        return new AmazonSqsQueue(queueUri, amazonSqsOptions, _cancellationTokenSource.Get().Token);
    }
}