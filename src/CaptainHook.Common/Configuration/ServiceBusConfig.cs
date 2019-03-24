using System;
using Microsoft.ServiceFabric.Data;

namespace CaptainHook.Common.Configuration
{
    public class ServiceBusConfig : IReliableState
    {
        public ServiceBusConfig()
        {
            this.Name = new Uri(nameof(ServiceBusConfig));
        }

        public ServiceBusConfig(Uri name)
        {
            this.Name = name;
        }
        public int BatchSize { get; set; } = 1;

        public string ServiceBusConnectionString { get; set; }

        public string ServiceBusNamespace;

        public string ServiceBusSubscriptionId;

        public string SubscriptionName = "captain-hook";

        public Uri Name { get; }
    }
}