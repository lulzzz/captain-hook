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
using Microsoft.ServiceFabric.Services.Runtime;

namespace CaptainHook.EventReaderService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class EventReaderService : StatefulService, IEventReaderService
    {
        private bool _configCalled;
        private readonly IBigBrother _bigBrother;
        private ServiceBusConfig _serviceBusConfig;
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

        public EventReaderService(StatefulServiceContext context, IBigBrother bigBrother)
            : base(context)
        {
            _bigBrother = bigBrother;
        }

        public void Configure(ServiceBusConfig serviceBusConfig, WebhookConfig webhookConfig)
        {
            if (serviceBusConfig == null)
            {
                throw new ArgumentNullException(nameof(serviceBusConfig));
            }

            if (string.IsNullOrWhiteSpace(serviceBusConfig.ServiceBusSubscriptionId))
            {
                throw new ArgumentNullException(nameof(serviceBusConfig.ServiceBusSubscriptionId));
            }

            if (string.IsNullOrWhiteSpace(serviceBusConfig.ServiceBusNamespace))
            {
                throw new ArgumentNullException(nameof(serviceBusConfig.ServiceBusNamespace));
            }

            if (string.IsNullOrWhiteSpace(webhookConfig.Type))
            {
                throw new ArgumentNullException(nameof(webhookConfig.Type));
            }

            if (string.IsNullOrWhiteSpace(serviceBusConfig.SubscriptionName))
            {
                throw new ArgumentNullException(nameof(serviceBusConfig.SubscriptionName));
            }

            _serviceBusConfig = serviceBusConfig;
            _webhookConfig = webhookConfig;
            _configCalled = true;
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

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            _bigBrother.Publish(new StatefulServiceActivatedEvent(this));

            if (!_configCalled)
            {
                throw new Exception("Event Reader Service was not configured before it started");
            }
            
            var topicConfig = await this.StateManager.TryGetAsync<ServiceBusConfig>(nameof(ServiceBusConfig));

            if (topicConfig.HasValue)
            {
                _serviceBusConfig = topicConfig.Value;
            }
            else
            {
                throw new ArgumentNullException(nameof(ServiceBusConfig), "Topic Config not initialized");
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
                .WithSubscription(_serviceBusConfig.ServiceBusSubscriptionId)
                .ServiceBusNamespaces.List()
                .SingleOrDefault(n => n.Name == _serviceBusConfig.ServiceBusNamespace);

            if (sbNamespace == null)
            {
                throw new InvalidOperationException($"Couldn't find the service bus namespace {_serviceBusConfig.ServiceBusNamespace} in the subscription with ID {_serviceBusConfig.ServiceBusSubscriptionId}");
            }

            var azureTopic = await sbNamespace.CreateTopicIfNotExists(_webhookConfig.Type);
            await azureTopic.CreateSubscriptionIfNotExists(_serviceBusConfig.SubscriptionName);

            _receiver = new MessageReceiver(
                _serviceBusConfig.ServiceBusConnectionString,
                EntityNameHelper.FormatSubscriptionPath(_webhookConfig.Type, _serviceBusConfig.SubscriptionName),
                ReceiveMode.PeekLock,
                new RetryExponential(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), 3),
                _serviceBusConfig.BatchSize);
        }

        public async Task ReadEvents(CancellationToken cancellationToken)
        {
            if (_receiver.IsClosedOrClosing) return;

            var messages = await _receiver.ReceiveAsync(_serviceBusConfig.BatchSize, TimeSpan.FromMilliseconds(50));
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

                await PersistMessageHandle(cancellationToken, messageData, handlerId, message);

                await ActorProxy.Create<IEventHandlerActor>(new ActorId(messageData.EventHandlerActorId)).HandleMessage(messageData);
            }
        }

        /// <summary>
        /// Persists the handle state so that we can continue with execution in case of failure or node change and eventually retry the message or delete it
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="messageData"></param>
        /// <param name="handlerId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task PersistMessageHandle(CancellationToken cancellationToken, MessageData messageData, int handlerId, Message message)
        {
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
        }
    }
}
