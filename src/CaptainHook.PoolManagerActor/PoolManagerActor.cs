﻿using Eshopworld.Core;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CaptainHook.Common;
using CaptainHook.Common.Telemetry;
using CaptainHook.Interfaces;

namespace CaptainHook.PoolManagerActor
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
    public class PoolManagerActor : Actor, IPoolManagerActor
    {
        private readonly IBigBrother _bigBrother;
        private HashSet<int> _free; // free pool resources
        private Dictionary<Guid, MessageHook> _busy; // busy pool resources

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
            _bigBrother.Publish(new ActorActivated(this));
            var free = await StateManager.TryGetStateAsync<HashSet<int>>(nameof(_free));

            if (free.HasValue)
            {
                var busy = await StateManager.TryGetStateAsync<Dictionary<Guid, MessageHook>>(nameof(_busy));
                if (busy.HasValue)
                {
                    _busy = busy.Value;
                }
            }
            else
            {
                _free = new HashSet<int>(NumberOfHandlers);
                _busy = new Dictionary<Guid, MessageHook>(NumberOfHandlers);

                for (var i = 0; i < NumberOfHandlers; i++)
                {
                    ActorProxy.Create<IEventHandlerActor>(new ActorId(i)); // TODO: this probably isn't needed here since we're not invoking the actor at this point - REVIEW
                    _free.Add(i);
                }

                await StateManager.AddOrUpdateStateAsync(nameof(_free), _free, (s, value) => value);
                await StateManager.AddOrUpdateStateAsync(nameof(_busy), _busy, (s, value) => value);
            }
        }

        protected override async Task OnDeactivateAsync()
        {
            _bigBrother.Publish(new ActorDeactivated(this));

            await StateManager.AddOrUpdateStateAsync(nameof(_free), _free, (s, value) => value);
            await StateManager.AddOrUpdateStateAsync(nameof(_busy), _busy, (s, value) => value);
        }

        public async Task<Guid> DoWork(string payload, string type)
        {
            // need to handle the possibility of the resources in the pool being all busy!

            try
            {
                var handlerId = _free.First();
                var handle = Guid.NewGuid();
                _free.Remove(handlerId);
                _busy.Add(handle, new MessageHook
                {
                    HandlerId = handlerId,
                    Type = type
                });

                await StateManager.AddOrUpdateStateAsync(nameof(_free), _free, (s, value) => value);
                await StateManager.AddOrUpdateStateAsync(nameof(_busy), _busy, (s, value) => value);

                await ActorProxy.Create<IEventHandlerActor>(new ActorId(handlerId)).Handle(handle, payload, type);

                return handle;
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
                if (_busy.ContainsKey(handle))
                {
                    var msgHook = _busy[handle];
                    _busy.Remove(handle);
                    _free.Add(msgHook.HandlerId);

                    await StateManager.AddOrUpdateStateAsync(nameof(_free), _free, (s, value) => value);
                    await StateManager.AddOrUpdateStateAsync(nameof(_busy), _busy, (s, value) => value);

                    await ActorProxy.Create<IEventReaderActor>(new ActorId(msgHook.Type)).CompleteMessage(handle);
                }
                else
                {
                    _bigBrother.Publish(new PoolManagerActorTelemetryEvent($"Key {handle} not found in the dictionary", this)
                    {
                        BusyHandlerCount = _busy.Count,
                        FreeHandlerCount = _free.Count
                    });
                }
            }
            catch (Exception e)
            {
                _bigBrother.Publish(e.ToExceptionEvent());
                throw;
            }
        }
    }
}
