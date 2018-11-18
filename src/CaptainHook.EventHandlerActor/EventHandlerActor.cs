namespace CaptainHook.EventHandlerActor
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Interfaces;
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
    public class EventHandlerActor : Actor, IEventHandlerActor
    {
        private IActorTimer _handleTimer;
        /// <summary>
        /// Initializes a new instance of EventHandlerActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public EventHandlerActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            return Task.CompletedTask;
        }

        public async Task Handle(Guid handle, string payload, string type)
        {
            var handlerMeta = new KeyValuePair<string, string>(type, payload);
            await StateManager.AddOrUpdateStateAsync(handle.ToString(), handlerMeta, (s, pair) => handlerMeta);

            _handleTimer = RegisterTimer(
                InternalHandle,
                null,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.MaxValue);
        }

        private async Task InternalHandle(object _)
        {
            await Task.Yield();
        }
    }
}
