namespace CaptainHook.EventReaderActor
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Common;
    using Common.Telemetry;
    using Eshopworld.Core;
    using Interfaces;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Azure.Services.AppAuthentication;
    using Microsoft.Rest;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;

    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    public class EventReaderActor : Actor, IEventReaderActor
    {
        private const string SubscriptionName = "captain-hook";

        // TAKE NUMBER OF HANDLERS INTO CONSIDERATION, DO NOT BATCH MORE THEN HANDLERS
        private const int BatchSize = 10; // make this configurable

        private readonly IBigBrother _bb;
        private readonly ConfigurationSettings _settings;
        private IActorTimer _poolTimer;
        private MessageReceiver _receiver;

        /// <summary>
        /// Initializes a new instance of EventReaderActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        /// <param name="bb">The <see cref="IBigBrother"/> telemetry instance that this actor instance will use to publish.</param>
        /// <param name="settings">The <see cref="ConfigurationSettings"/> being read from the KeyVault.</param>
        public EventReaderActor(ActorService actorService, ActorId actorId, IBigBrother bb, ConfigurationSettings settings)
            : base(actorService, actorId)
        {
            _bb = bb;
            _settings = settings;
        }

        protected override async Task OnActivateAsync()
        {
            _bb.Publish(new ActorActivated(this));
            await SetupServiceBus();

            _poolTimer = RegisterTimer(
                ReadEvents,
                null,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(100));

            _receiver = new MessageReceiver(
                _settings.ServiceBusConnectionString,
                EntityNameHelper.FormatSubscriptionPath(TypeExtensions.GetEntityName(Id.GetStringId()), SubscriptionName),
                ReceiveMode.PeekLock,
                new RetryExponential(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), 3),
                BatchSize);

            await base.OnActivateAsync();
        }

        protected override Task OnDeactivateAsync()
        {
            if (_poolTimer != null)
            {
                UnregisterTimer(_poolTimer);
            }

            return base.OnDeactivateAsync();
        }

        public async Task Run()
        {
            await Task.Yield();
        }

        public async Task CompleteMessage(Guid id)
        {
            await Task.Yield();
        }

        internal async Task SetupServiceBus()
        {
            var token = new AzureServiceTokenProvider().GetAccessTokenAsync("https://management.core.windows.net/", string.Empty).Result;
            var tokenCredentials = new TokenCredentials(token);

            var client = RestClient.Configure()
                                   .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                                   .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                                   .WithCredentials(new AzureCredentials(tokenCredentials, tokenCredentials, string.Empty, AzureEnvironment.AzureGlobalCloud))
                                   .Build();

            var sbNamespace = Azure.Authenticate(client, string.Empty)
                                   .WithSubscription(_settings.AzureSubscriptionId)
                                   .ServiceBusNamespaces.List()
                                   .SingleOrDefault(n => n.Name == _settings.ServiceBusNamespace);

            if (sbNamespace == null)
            {
                throw new InvalidOperationException($"Couldn't find the service bus namespace {_settings.ServiceBusNamespace} in the subscription with ID {_settings.AzureSubscriptionId}");
            }

            var azureTopic = await sbNamespace.CreateTopicIfNotExists(TypeExtensions.GetEntityName(Id.GetStringId()));
            await azureTopic.CreateSubscriptionIfNotExists(SubscriptionName);
        }

        internal async Task ReadEvents(object _)
        {
            if (_receiver.IsClosedOrClosing) return;

            var messages = await _receiver.ReceiveAsync(BatchSize).ConfigureAwait(false);
            if (messages == null) return;

            // Do stuff with messages
        }

        public async Task CompleteWork(Guid id)
        {
            await Task.Yield();
            throw new NotImplementedException();
        }
    }
}
