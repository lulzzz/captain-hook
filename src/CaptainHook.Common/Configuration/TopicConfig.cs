using System;
using Microsoft.ServiceFabric.Data;

namespace CaptainHook.Common.Configuration
{
    public class TopicConfig : IReliableState
    {
        public TopicConfig()
        {
            this.Name = new Uri(nameof(TopicConfig));
        }

        public TopicConfig(Uri name)
        {
            this.Name = name;
        }
        public int BatchSize { get; set; } = 1;

        public string ServiceBusConnectionString { get; set; }

        public string ServiceBusNamespace;

        public string ServiceBusSubscriptionId;

        public string ServiceBusTopicName;

        public string SubscriptionName = "captain-hook";

        public Uri Name { get; }
    }
}