using System;
using System.Fabric;
using System.Fabric.Description;
using System.Threading;
using System.Threading.Tasks;
using CaptainHook.Common.Configuration;
using CaptainHook.Common.Telemetry;
using CaptainHook.Common.Telemetry.Actor;
using CaptainHook.Interfaces;
using Eshopworld.Core;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace CaptainHook.MessagingDirectorActorService
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
    public class MessagingDirectorActor : Actor, IMessagingDirector
    {
        private readonly IBigBrother _bigBrother;
        private readonly ServiceBusConfig _serviceBusConfig;

        /// <summary>
        /// Initializes a new instance of <see cref="MessagingDirectorActor"/>.
        /// </summary>
        /// <param name="actorService">The <see cref="ActorService"/> that will host this actor instance.</param>
        /// <param name="actorId">The <see cref="ActorId"/> for this actor instance.</param>
        /// <param name="bigBrother"></param>
        /// <param name="serviceBusConfig"></param>
        public MessagingDirectorActor(
            ActorService actorService,
            ActorId actorId,
            IBigBrother bigBrother,
            ServiceBusConfig serviceBusConfig)
            : base(actorService, actorId)
        {
            this._bigBrother = bigBrother;
            _serviceBusConfig = serviceBusConfig;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override Task OnActivateAsync()
        {
            _bigBrother.Publish(new ActorActivatedEvent(this));
            return base.OnActivateAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override Task OnDeactivateAsync()
        {
            _bigBrother.Publish(new ActorDeactivatedEvent(this));
            return base.OnDeactivateAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task Run()
        {
            foreach (var domainType in await this.StateManager.GetStateNamesAsync(CancellationToken.None))
            {
                var webhookConfig = await ReadWebhookAsync(domainType);
                var proxy = ServiceProxy.Create<IEventReaderService>(new Uri("fabric:/CaptainHook/CaptainHook.EventReaderService"), 
                    ServicePartitionKey.Singleton);
                proxy.Configure(_serviceBusConfig, webhookConfig);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<WebhookConfig> ReadWebhookAsync(string name)
        {
            var conditionalValue = await StateManager.TryGetStateAsync<WebhookConfig>(name, CancellationToken.None);
            return conditionalValue.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public async Task<WebhookConfig> CreateWebhookAsync(WebhookConfig config)
        {
            var result = await StateManager.TryAddStateAsync(config.Type, config, CancellationToken.None);

            if (!result)
            {
                _bigBrother.Publish(new ActorStateEvent(config.Type, "Cannot create new Webhook. Webhook with the same name has already been created"));
                throw new Exception("Cannot create new Webhook. Webhook with the same name has already been created");
            }

            _bigBrother.Publish(new WebHookCreatedEvent(config.Type));

            using (var client = new FabricClient())
            {

            }

                var proxy = ServiceProxy.Create<IEventReaderService>(new Uri("fabric:/CaptainHook/CaptainHook.EventReaderService"), ServicePartitionKey.Singleton);
            proxy.Configure(_serviceBusConfig, config);

            return await StateManager.GetStateAsync<WebhookConfig>(config.Type, CancellationToken.None);
        }

        public WebhookConfig UpdateWebhook(WebhookConfig config)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DeleteWebhookAsync(string type, CancellationToken cancellationToken)
        {
            var result = await StateManager.TryGetStateAsync<WebhookConfig>(type, CancellationToken.None);

            if (result.HasValue)
            {
                _bigBrother.Publish(new ActorDeletedEvent(type, "Deleting actor based on api request"));

                using (var client = new FabricClient())
                {
                    await client.ServiceManager.DeleteServiceAsync(new DeleteServiceDescription(new Uri("fabric:/CaptainHook/CaptainHook.EventReaderService")), TimeSpan.FromMinutes(30), cancellationToken);
                }

                await StateManager.RemoveStateAsync(type, cancellationToken);
            }

            throw new Exception("actor not found");
        }
    }
}
