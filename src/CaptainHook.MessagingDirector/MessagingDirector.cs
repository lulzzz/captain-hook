using System;
using System.Threading;
using CaptainHook.Common.Configuration;
using CaptainHook.Common.Telemetry;
using CaptainHook.Common.Telemetry.Actor;
using Eshopworld.Core;

namespace CaptainHook.MessagingDirector
{
    using System.Threading.Tasks;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
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
    public class MessagingDirector : Actor, IMessagingDirector
    {
        private readonly IBigBrother _bigBrother;

        /// <summary>
        /// Initializes a new instance of <see cref="MessagingDirector"/>.
        /// </summary>
        /// <param name="actorService">The <see cref="ActorService"/> that will host this actor instance.</param>
        /// <param name="actorId">The <see cref="ActorId"/> for this actor instance.</param>
        /// <param name="bigBrother"></param>
        public MessagingDirector(
            ActorService actorService,
            ActorId actorId,
            IBigBrother bigBrother)
            : base(actorService, actorId)
        {
            this._bigBrother = bigBrother;
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
            foreach (var type in await this.StateManager.GetStateNamesAsync(CancellationToken.None))
            {
                await ActorProxy.Create<IEventReaderActor>(new ActorId(type)).Run();
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
            await ActorProxy.Create<IEventReaderActor>(new ActorId(config.Type)).Run();

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
        /// <returns></returns>
        public async Task DeleteWebhookAsync(string type)
        {
            var result = await StateManager.TryGetStateAsync<WebhookConfig>(type, CancellationToken.None);

            if (result.HasValue)
            {
                _bigBrother.Publish(new ActorDeletedEvent(type, "Deleting actor based on api request"));

                var actorId = new ActorId(type);
                //todo if we move message state into the fabric then we have to consider that we might need to delete any state the actor has when we delete it.
                //todo will we have to clean up any start for any other actor in the chain

                //todo clean up naming here should be in naming class
                var actorProxy = ActorServiceProxy.Create(new Uri("fabric:/CaptainHook/EventReaderActor"), actorId);
                await actorProxy.DeleteActorAsync(actorId, CancellationToken.None);
            }

            throw new Exception("actor not found");
        }
    }
}
