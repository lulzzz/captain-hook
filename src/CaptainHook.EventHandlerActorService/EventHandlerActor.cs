using System;
using System.Threading.Tasks;
using CaptainHook.Common;
using CaptainHook.Common.Telemetry.Actor;
using CaptainHook.EventHandlerActorService.Handlers;
using CaptainHook.Interfaces;
using Eshopworld.Core;
using Eshopworld.Telemetry;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace CaptainHook.EventHandlerActorService
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
    public class EventHandlerActor : Actor, IEventHandlerActor
    {
        private readonly IEventHandlerFactory _eventHandlerFactory;
        private readonly IBigBrother _bigBrother;
        private IActorTimer _handleTimer;

        /// <summary>
        /// Initializes a new instance of EventHandlerActor
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        /// <param name="eventHandlerFactory"></param>
        /// <param name="bigBrother"></param>
        public EventHandlerActor(
            ActorService actorService,
            ActorId actorId,
            IEventHandlerFactory eventHandlerFactory,
            IBigBrother bigBrother)
            : base(actorService, actorId)
        {
            _eventHandlerFactory = eventHandlerFactory;
            _bigBrother = bigBrother;
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override async Task OnActivateAsync()
        {
            _bigBrother.Publish(new ActorActivatedEvent(this));
            if ((await StateManager.TryGetStateAsync<MessageData>(nameof(EventHandlerActor))).HasValue)
            {
                // There's a message to handle, but we're not sure if it was fully handled or not, so we are going to handle it anyways
                // Assuming whatever I'm calling can handle idempotency

                _handleTimer = RegisterTimer(
                    InternalHandle,
                    null,
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.MaxValue);
            }
        }

        /// <summary>
        /// On deactivating actor call and shutdown the actor.
        /// </summary>
        /// <returns></returns>
        protected override Task OnDeactivateAsync()
        {
            _bigBrother.Publish(new ActorDeactivatedEvent(this));
            return base.OnDeactivateAsync();
        }

        /// <summary>
        /// Here we're just adding the message to the actors work load and returning. We don't want to block the whole chain of actors
        /// </summary>
        /// <param name="messageData"></param>
        /// <returns></returns>
        public async Task HandleMessage(MessageData messageData)
        {
            await StateManager.AddOrUpdateStateAsync(nameof(EventHandlerActor), messageData, (s, pair) => pair);
        }

        /// <remarks>
        /// Not used in this case, because we are hard-coding all handling logic in this Actor, so there's no need to handle completion in higher actors.
        /// </remarks>>
        public async Task CompleteDispatch(string baseUri)
        {
            await Task.Yield();
        }

        /// <summary>
        /// The messages are actually processed in here and we need to allow the timer to calls this so that the call to this actor is decoupled from the calling of this actor downstream
        /// </summary>
        /// <param name="_"></param>
        /// <returns></returns>
        private async Task InternalHandle(object _)
        {
            try
            {
                UnregisterTimer(_handleTimer);

                var messageData = await StateManager.TryGetStateAsync<MessageData>(nameof(EventHandlerActor));
                if (!messageData.HasValue)
                {
                    _bigBrother.Publish(new Exception("message was empty"));
                    return;
                }

                var handler = _eventHandlerFactory.CreateWebhookWithCallbackHandler(messageData.Value.Type, messageData.Value.WebhookConfig);
                await handler.Call(messageData.Value);

                await StateManager.RemoveStateAsync(nameof(EventHandlerActor));

                //todo clean up this so type is based on 
                await ActorProxy.Create<IPoolManagerActor>(new ActorId(0)).CompleteWork(messageData.Value.Handle);
            }
            catch (Exception e)
            {
                BigBrother.Write(e.ToExceptionEvent());
            }
        }
    }
}
