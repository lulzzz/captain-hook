using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CaptainHook.Common;
using CaptainHook.Common.Configuration;
using CaptainHook.Common.Telemetry.Services;
using CaptainHook.Interfaces;
using Eshopworld.Core;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Runtime;

namespace CaptainHook.EventReader
{
    public interface IEventReader : IService
    {
        void Configure(TopicConfig topicConfig, WebhookConfig webhookConfig);

        Task CompleteMessage(Guid handle);
    }

    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class EventReader : StatefulService, IEventReader
    {
        private readonly IBigBrother _bigBrother;
        private TopicConfig _topicConfig;
        private WebhookConfig _webhookConfig;
        private MessageReceiver _receiver;

        private readonly Dictionary<Guid, string> _lockTokens = new Dictionary<Guid, string>();
        private readonly Dictionary<Guid, int> _inFlightMessages = new Dictionary<Guid, int>();
        private readonly HashSet<int> _freeHandlers = new HashSet<int>();

#if DEBUG
        private int _handlerCount = 1;
#else
        private int _handlerCount = 10;
#endif

        public EventReader(StatefulServiceContext context, IBigBrother bigBrother)
            : base(context)
        {
            _bigBrother = bigBrother;
        }

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

        public void Configure(TopicConfig topicConfig, WebhookConfig webhookConfig)
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

            _topicConfig = topicConfig;
            _webhookConfig = webhookConfig;
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            _bigBrother.Publish(new StatefulServiceActivatedEvent(this));
            var topicConfig = await this.StateManager.TryGetAsync<TopicConfig>(nameof(TopicConfig));

            if (topicConfig.HasValue)
            {
                _topicConfig = topicConfig.Value;
            }
            else
            {
                throw new ArgumentNullException(nameof(TopicConfig), "Topic Config not initialized");
            }

            //await BuildInMemoryState();
            await SetupServiceBus();

            while (!cancellationToken.IsCancellationRequested)
            {
                await ReadEvents(cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        protected override Task OnCloseAsync(CancellationToken cancellationToken)
        {
            _bigBrother.Publish(new StatefulServiceDeactivatedEvent(this));

            return base.OnCloseAsync(cancellationToken);
        }

        public async Task CompleteMessage(Guid handle)
        {
            await _receiver.CompleteAsync(_lockTokens[handle]);
            await RemoveHandle(handle);
        }

        private async Task RemoveHandle(Guid handle)
        {
            _lockTokens.Remove(handle);
            _inFlightMessages.Remove(handle);

            using (var tx = StateManager.CreateTransaction())
            {
                await StateManager.RemoveAsync(tx, handle.ToString());
                await tx.CommitAsync();
            }

            //todo message deleted event
        }

        private async Task SetupServiceBus()
        {
            var token = new AzureServiceTokenProvider().GetAccessTokenAsync("https://management.core.windows.net/", string.Empty).Result;
            var tokenCredentials = new TokenCredentials(token);

            var client = RestClient.Configure()
                .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .WithCredentials(new AzureCredentials(tokenCredentials, tokenCredentials, string.Empty, AzureEnvironment.AzureGlobalCloud))
                .Build();

            var sbNamespace = Azure.Authenticate(client, string.Empty)
                .WithSubscription(_topicConfig.ServiceBusSubscriptionId)
                .ServiceBusNamespaces.List()
                .SingleOrDefault(n => n.Name == _topicConfig.ServiceBusNamespace);

            if (sbNamespace == null)
            {
                throw new InvalidOperationException($"Couldn't find the service bus namespace {_topicConfig.ServiceBusNamespace} in the subscription with ID {_topicConfig.ServiceBusSubscriptionId}");
            }

            var azureTopic = await sbNamespace.CreateTopicIfNotExists(_topicConfig.ServiceBusTopicName);
            await azureTopic.CreateSubscriptionIfNotExists(_topicConfig.SubscriptionName);

            _receiver = new MessageReceiver(
                _topicConfig.ServiceBusConnectionString,
                EntityNameHelper.FormatSubscriptionPath(_topicConfig.ServiceBusTopicName, _topicConfig.SubscriptionName),
                ReceiveMode.PeekLock,
                new RetryExponential(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), 3),
                _topicConfig.BatchSize);
        }

        public async Task ReadEvents(CancellationToken cancellationToken)
        {
            if (_receiver.IsClosedOrClosing) return;

            var messages = await _receiver.ReceiveAsync(_topicConfig.BatchSize, TimeSpan.FromMilliseconds(50));
            if (messages == null) return;

            foreach (var message in messages)
            {
                var messageData = new MessageData(Encoding.UTF8.GetString(message.Body), Context.ServiceTypeName);

                var handlerId = _freeHandlers.FirstOrDefault();
                if (handlerId == 0)
                {
                    handlerId = ++_handlerCount;
                }
                else
                {
                    _freeHandlers.Remove(handlerId);
                }

                messageData.HandlerId = handlerId;
                messageData.WebhookConfig = _webhookConfig;
                _inFlightMessages.Add(messageData.Handle, handlerId);
                _lockTokens.Add(messageData.Handle, message.SystemProperties.LockToken);

                var handleData = new MessageDataHandle
                {
                    Handle = messageData.Handle,
                    HandlerId = handlerId,
                    LockToken = message.SystemProperties.LockToken
                };

                var data = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, MessageDataHandle>>("handles");
                using (var tx = StateManager.CreateTransaction())
                {
                    await data.AddAsync(tx, messageData.Handle, handleData, TimeSpan.FromSeconds(30), cancellationToken);
                    await tx.CommitAsync();
                }

                await ActorProxy.Create<IEventHandlerActor>(new ActorId(messageData.EventHandlerActorId)).HandleMessage(messageData);
            }
        }
    }
}
