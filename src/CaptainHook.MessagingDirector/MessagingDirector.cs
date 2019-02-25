using System.Threading;
using CaptainHook.Common.Configuration;
using CaptainHook.Common.Telemetry;
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
            _bigBrother.Publish(new ActorActivated(this));
            return base.OnActivateAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override Task OnDeactivateAsync()
        {
            _bigBrother.Publish(new ActorDeactivated(this));
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
                return await this.StateManager.GetStateAsync<WebhookConfig>(config.Type, CancellationToken.None);
            }

            this._bigBrother.Publish(new WebHookCreated(config.Type));
            await ActorProxy.Create<IEventReaderActor>(new ActorId(config.Type)).Run();

            return await StateManager.GetStateAsync<WebhookConfig>(config.Type, CancellationToken.None);
        }

        public WebhookConfig UpdateWebhook(WebhookConfig config)
        {
            throw new System.NotImplementedException();
        }

        public void DeleteWebhook(string name)
        {
            throw new System.NotImplementedException();
        }
    }
}
