using Eshopworld.Core;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace CaptainHook.Common.Telemetry.Actor
{
    public class ActorActivatedEvent : TelemetryEvent
    {
        public string ActorName { get; set; }

        public string ActorId { get; set; }

        public ActorActivatedEvent(ActorBase actor)
        {
            ActorId = actor.Id.ToString();
            ActorName = actor.ActorService.ActorTypeInformation.ServiceName;
        }
    }
}
