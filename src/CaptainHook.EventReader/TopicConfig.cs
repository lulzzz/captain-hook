using System;
using Microsoft.ServiceFabric.Data;

namespace CaptainHook.EventReader
{
    public class TopicConfig : IReliableState
    {
        public TopicConfig()
        {
            Name = new Uri(nameof(TopicConfig));
        }

        public TopicConfig(Uri name)
        {
            Name = name;
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