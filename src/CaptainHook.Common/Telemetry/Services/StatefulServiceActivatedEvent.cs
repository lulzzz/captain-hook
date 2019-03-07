using Eshopworld.Core;
using Microsoft.ServiceFabric.Services.Runtime;

namespace CaptainHook.Common.Telemetry.Services
{
    public class StatefulServiceActivatedEvent : TelemetryEvent
    {
        public StatefulServiceActivatedEvent(StatefulService service)
        {
            //todo check this is what I expect the value to be.
            Type = service.Context.ServiceTypeName;
            Name = service.Context.ServiceName.AbsoluteUri;
        }

        public string Type { get; set; }

        public string Name { get; set; }
    }
}
