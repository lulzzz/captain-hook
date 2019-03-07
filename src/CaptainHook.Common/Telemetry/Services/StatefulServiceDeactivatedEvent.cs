using Microsoft.ServiceFabric.Services.Runtime;

namespace CaptainHook.Common.Telemetry.Services
{
    public class StatefulServiceDeactivatedEvent : StatefulServiceActivatedEvent
    {
        public StatefulServiceDeactivatedEvent(StatefulService service) : base(service)
        {

        }
    }
}
