using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Runtime;

namespace CaptainHook.EventReader
{
    public class TopicConfig
    {
        public string ServiceBusNamespace;

        public string ServiceBusSubscriptionId;

        public string ServiceBusTopicName;

        public string SubscriptionName = "captain-hook";
    }

    public interface IEventReader : IService
    {
        void Configure(TopicConfig topicConfig);
    }

    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class EventReader : StatefulService, IEventReader
    {
        private TopicConfig _config;

        public EventReader(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await Startup(_config);
            
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private static async Task Startup(TopicConfig config)
        {
            var token = new AzureServiceTokenProvider().GetAccessTokenAsync("https://management.core.windows.net/", string.Empty).Result;
            var tokenCredentials = new TokenCredentials(token);

            var client = RestClient.Configure()
                .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .WithCredentials(new AzureCredentials(tokenCredentials, tokenCredentials, string.Empty, AzureEnvironment.AzureGlobalCloud))
                .Build();

            var sbNamespace = Azure.Authenticate(client, string.Empty)
                .WithSubscription(config.ServiceBusSubscriptionId)
                .ServiceBusNamespaces.List()
                .SingleOrDefault(n => n.Name == config.ServiceBusNamespace);

            if (sbNamespace == null)
            {
                throw new InvalidOperationException($"Couldn't find the service bus namespace {config.ServiceBusNamespace} in the subscription with ID {config.ServiceBusSubscriptionId}");
            }

            var azureTopic = await sbNamespace.CreateTopicIfNotExists(config.ServiceBusTopicName);
            await azureTopic.CreateSubscriptionIfNotExists(config.SubscriptionName);
        }

        public void Configure(TopicConfig topicConfig)
        {
            if (topicConfig == null)
            {
                throw new ArgumentNullException(nameof(topicConfig));
            }

            if (string.IsNullOrWhiteSpace(topicConfig.ServiceBusSubscriptionId))
            {
                throw new ArgumentNullException(nameof(topicConfig.ServiceBusSubscriptionId));
            }

            if (string.IsNullOrWhiteSpace(topicConfig.ServiceBusNamespace))
            {
                throw new ArgumentNullException(nameof(topicConfig.ServiceBusNamespace));
            }

            if (string.IsNullOrWhiteSpace(topicConfig.ServiceBusTopicName))
            {
                throw new ArgumentNullException(nameof(topicConfig.ServiceBusTopicName));
            }

            if (string.IsNullOrWhiteSpace(topicConfig.SubscriptionName))
            {
                throw new ArgumentNullException(nameof(topicConfig.SubscriptionName));
            }

            _config = topicConfig;
        }
    }
}
