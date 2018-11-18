namespace CaptainHook.PoolManagerActor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using Eshopworld.Core;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Data;

    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class PoolManagerActor : Actor, IPoolManagerActor
    {
        private readonly IBigBrother _bigBrother;
        private ConditionalValue<HashSet<int>> _free; // free pool resources
        private ConditionalValue<Dictionary<Guid, int>> _busy; // busy pool resources

        private const int NumberOfHandlers = 10; // TODO: TWEAK THIS - HARDCODED FOR NOW

        /// <summary>
        /// Initializes a new instance of PoolManagerActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        /// <param name="bigBrother">The <see cref="IBigBrother"/> instanced used to publish telemetry.</param>
        public PoolManagerActor(ActorService actorService, ActorId actorId, IBigBrother bigBrother)
            : base(actorService, actorId)
        {
            _bigBrother = bigBrother;
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            _free = await StateManager.TryGetStateAsync<HashSet<int>>("free");

            if (_free.HasValue)
            {
                _busy = await StateManager.TryGetStateAsync<Dictionary<Guid, int>>(nameof(_busy));
            }
            else
            {
                _free = new ConditionalValue<HashSet<int>>(true, new HashSet<int>());
                _busy = new ConditionalValue<Dictionary<Guid, int>>(true, new Dictionary<Guid, int>());

                for (var i = 0; i < NumberOfHandlers; i++)
                {
                    ActorProxy.Create<IEventHandlerActor>(new ActorId(i));
                    _free.Value.Add(i);
                }

                await StateManager.AddOrUpdateStateAsync(nameof(_free), _free, (s, value) => _free);
                await StateManager.AddOrUpdateStateAsync(nameof(_busy), _busy, (s, value) => _busy);
            }
        }

        public async Task<Guid> DoWork(string payload, string type)
        {
            // need to handle the possibility of the resources in the pool being all busy!

            using (var scope = new TransactionScope())
            {
                try
                {
                    var handlerId = _free.Value.First();
                    var handle = Guid.NewGuid();
                    _free.Value.Remove(handlerId);
                    _busy.Value.Add(handle, handlerId);

                    await StateManager.AddOrUpdateStateAsync(nameof(_free), _free, (s, value) => _free);
                    await StateManager.AddOrUpdateStateAsync(nameof(_busy), _busy, (s, value) => _busy);

                    scope.Complete();

                    return handle;
                }
                catch (Exception e)
                {
                    _bigBrother.Publish(e.ToExceptionEvent());
                    scope.Dispose();
                    throw;
                }
            }
        }
    }
}
