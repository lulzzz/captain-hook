namespace CaptainHook.PoolManagerActor
{
    using Common;
    using Eshopworld.Core;
    using Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Data;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    public class PoolManagerActor : Actor, IPoolManagerActor
    {
#if DEBUG
        private const int NumberOfStartingHandlers = 1;
#else
        private const int NumberOfStartingHandlers = 10;
#endif

        private readonly IBigBrother _bigBrother;
        private ConditionalValue<HashSet<int>> _free; // free pool resources
        private ConditionalValue<Dictionary<Guid, MessageData>> _busy; // busy pool resources
        private int HandlerCount = NumberOfStartingHandlers;
        
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
                _busy = await StateManager.TryGetStateAsync<Dictionary<Guid, MessageData>>(nameof(_busy));
            }
            else
            {
                _free = new ConditionalValue<HashSet<int>>(true, new HashSet<int>());
                _busy = new ConditionalValue<Dictionary<Guid, MessageData>>(true, new Dictionary<Guid, MessageData>());

                for (var i = 0; i < NumberOfStartingHandlers; i++)
                {
                    //ActorProxy.Create<IEventHandlerActor>(new ActorId($"{Id}-{i}"));
                    _free.Value.Add(i);
                }

                await StateManager.AddOrUpdateStateAsync(nameof(_free), _free, (s, value) => value);
                await StateManager.AddOrUpdateStateAsync(nameof(_busy), _busy, (s, value) => value);
            }
        }

        public async Task<Guid> DoWork(MessageData messageData)
        {
            try
            {
                if (_free.Value.Any())
                {
                    messageData.HandlerId = _free.Value.First();
                    _free.Value.Remove(messageData.HandlerId);
                }
                else
                {
                    //ActorProxy.Create<IEventHandlerActor>(new ActorId($"{Id}-{++HandlerCount}")); // change ++ behaviour
                    messageData.HandlerId = ++HandlerCount;
                }

                _busy.Value.Add(messageData.Handle, messageData);

                await StateManager.AddOrUpdateStateAsync(nameof(_free), _free, (s, value) => value);
                await StateManager.AddOrUpdateStateAsync(nameof(_busy), _busy, (s, value) => value);

                await ActorProxy.Create<IDoWork>(new ActorId($"{Id}-{messageData.HandlerId}"), null, nameof(IEventHandlerActor), null).DoWork(messageData);
                return messageData.Handle;
            }
            catch (Exception e)
            {
                _bigBrother.Publish(e.ToExceptionEvent());
                throw;
            }
        }

        public async Task CompleteWork(Guid handle)
        {
            try
            {
                var msgHook = _busy.Value[handle];
                _busy.Value.Remove(handle);
                _free.Value.Add(msgHook.HandlerId);

                await StateManager.AddOrUpdateStateAsync(nameof(_free), _free, (s, value) => value);
                await StateManager.AddOrUpdateStateAsync(nameof(_busy), _busy, (s, value) => value);

                await ActorProxy.Create<IEventReaderActor>(new ActorId(msgHook.Type)).CompleteMessage(handle);
            }
            catch (Exception e)
            {
                _bigBrother.Publish(e.ToExceptionEvent());
                throw;
            }
        }
    }
}
