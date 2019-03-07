using Eshopworld.Core;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace CaptainHook.Common.Telemetry.Actor
{
    public class ActorDeactivatedEvent : ActorActivatedEvent
    {
        public ActorDeactivatedEvent(ActorBase actor) : base(actor)
        {
            ActorId = actor.Id.ToString();
            ActorName = actor.ActorService.ActorTypeInformation.ServiceName;
        }
    }

    public class ActorDeletedEvent : TelemetryEvent
    {
        public ActorDeletedEvent(string Id, string message)
        {
            
        }

        public string Id { get; set; }

        public string Message { get; set; }
    }
}
