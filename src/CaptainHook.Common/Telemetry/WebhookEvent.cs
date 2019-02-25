using System;
using Eshopworld.Core;

namespace CaptainHook.Common.Telemetry
{
    public class WebhookEvent : TelemetryEvent
    {
        public WebhookEvent(string payload, string state = "failure")
        {
            Payload = payload;
            State = state;
        }

        public WebhookEvent(Guid handle, string type, string payload, string state = "success")
        {
            Handle = handle;
            Payload = payload;
            Type = type;
        }

        public Guid Handle { get; set; }

        public string Type { get; set; }

        public string Payload { get; set; }

        public string State { get; set; }
    }

    public class WebHookCreated : TelemetryEvent
    {
        public WebHookCreated(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }

    public class WebHookDeleted : TelemetryEvent
    {

    }

    public class WebHookUpdated : TelemetryEvent
    {

    }
}