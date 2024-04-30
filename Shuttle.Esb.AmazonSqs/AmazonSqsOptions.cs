using System;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AmazonSqs
{
    public class AmazonSqsOptions
    {
        public const string SectionName = "Shuttle:AmazonSqs";

        public string ServiceUrl { get; set; }
        public int MaxMessages { get; set; } = 10;
        public TimeSpan WaitTime { get; set; } = TimeSpan.FromSeconds(20);

        public event EventHandler<ConfigureEventArgs> Configure;

        public void OnConfigureConsumer(object sender, ConfigureEventArgs args)
        {
            Guard.AgainstNull(sender, nameof(sender));
            Guard.AgainstNull(args, nameof(args));

            Configure?.Invoke(sender, args);
        }
    }
}