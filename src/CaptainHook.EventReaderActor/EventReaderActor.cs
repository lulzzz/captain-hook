using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CaptainHook.Common;
using CaptainHook.Common.Configuration;
using CaptainHook.Common.Telemetry;
using CaptainHook.Common.Telemetry.Actor;
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
using Microsoft.ServiceFabric.Actors.Runtime;

namespace CaptainHook.EventReaderActor
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    public class EventReaderActor : Actor, IEventReaderActor, IRemindable
    {
        private const string SubscriptionName = "captain-hook";

        // TAKE NUMBER OF HANDLERS INTO CONSIDERATION, DO NOT BATCH MORE THEN HANDLERS
        private const int BatchSize = 1; // make this configurable

        private readonly IBigBrother _bigBrother;
        private readonly ConfigurationSettings _settings;
        private readonly object _gate = new object();

        private volatile bool _readingEvents;


#if DEBUG
        internal int HandlerCount = 1;
#else
        internal int HandlerCount = 10;
#endif

        /// <summary>
        /// Initializes a new instance of EventReaderActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        /// <param name="bigBrother">The <see cref="IBigBrother"/> telemetry instance that this actor instance will use to publish.</param>
        /// <param name="settings">The <see cref="ConfigurationSettings"/> being read from the KeyVault.</param>
        public EventReaderActor(ActorService actorService, ActorId actorId, IBigBrother bigBrother, ConfigurationSettings settings)
            : base(actorService, actorId)
        {
            _bigBrother = bigBrother;
            _settings = settings;
        }

        protected override async Task OnActivateAsync()
        {
            try
            {
                _bigBrother.Publish(new ActorActivatedEvent(this));

            }
            catch (Exception e)
            {
                _bigBrother.Publish(e.ToExceptionEvent());
                throw;
            }

            await base.OnActivateAsync();
        }

        protected override Task OnDeactivateAsync()
        {
            _bigBrother.Publish(new ActorDeactivatedEvent(this));
            UnregisterReminderAsync(_wakeupReminder);

            _poolTimer?.Dispose();
            return base.OnDeactivateAsync();
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



        /// <remarks>
        /// Do nothing by design. We just need to make sure that the actor is properly activated.
        /// </remarks>>
        public async Task Run()
        {
            //todo refactor out and put into a reliable service
            _wakeupReminder = await this.RegisterReminderAsync(
                WakeUpReminderName,
                null,
                TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(15));
        }

        public async Task CompleteMessage(Guid handle)
        {
            await _receiver.CompleteAsync(LockTokens[handle]);
            await RemoveHandle(handle);
        }

        public async Task FailMessage(Guid handle)
        {
            // TODO: do stuff but do nothing for now, the service bus will move the msg to deadletter

            await RemoveHandle(handle);
        }

        private async Task RemoveHandle(Guid handle)
        {
            LockTokens.Remove(handle);
            InFlightMessages.Remove(handle);
            await StateManager.RemoveStateAsync(handle.ToString());
        }
    }
}
