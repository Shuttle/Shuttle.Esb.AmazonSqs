using System;
using Shuttle.Core.Contract;

namespace Shuttle.Esb.AmazonSqs
{
    public class AmazonSqsOptions
    {
        public const string SectionName = "Shuttle:ServiceBus:AmazonSqs";

        public string ServiceUrl { get; set; }
        public int MaxMessages { get; set; }
        public TimeSpan WaitTime { get; set; }

        public event EventHandler<ConfigureEventArgs> Configure = delegate
        {
        };

        public void OnConfigureConsumer(object sender, ConfigureEventArgs args)
        {
            Guard.AgainstNull(sender, nameof(sender));
            Guard.AgainstNull(args, nameof(args));

            Configure.Invoke(sender, args);
        }

    }
}