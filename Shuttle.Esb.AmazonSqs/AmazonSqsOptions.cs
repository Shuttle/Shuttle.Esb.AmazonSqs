using System;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AmazonSqs;

public class AmazonSqsOptions
{
    public const string SectionName = "Shuttle:AmazonSqs";
    public int MaxMessages { get; set; } = 10;
    public string ServiceUrl { get; set; } = string.Empty;
    public TimeSpan WaitTime { get; set; } = TimeSpan.FromSeconds(20);

    public event EventHandler<ConfigureEventArgs>? Configure;

    public void OnConfigureConsumer(object? sender, ConfigureEventArgs args)
    {
        Configure?.Invoke(Guard.AgainstNull(sender), Guard.AgainstNull(args));
    }
}