using Eshopworld.Core;

namespace CaptainHook.Common.Telemetry
{
    public class ActorStateEvent : TelemetryEvent
    {
        public ActorStateEvent(string actorId, string message)
        {
            ActorName = actorId;
            ActorId = ActorId;
            Message = message;
        }

        public string ActorName { get; set; }

        public string ActorId { get; set; }

        public string Message { get; set; }
    }
}