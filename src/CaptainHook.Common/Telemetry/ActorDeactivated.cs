using Microsoft.ServiceFabric.Actors.Runtime;

namespace CaptainHook.Common.Telemetry
{
    public class ActorDeactivated : ActorActivated
    {
        public ActorDeactivated(ActorBase actor) : base(actor)
        {
            ActorId = actor.Id.ToString();
            ActorName = actor.ActorService.ActorTypeInformation.ServiceName;
        }
    }
}
