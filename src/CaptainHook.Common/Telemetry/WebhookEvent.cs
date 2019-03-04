using System;
using System.Net;
using Eshopworld.Core;

namespace CaptainHook.Common.Telemetry
{
    public class WebhookEvent : TelemetryEvent
    {
        public WebhookEvent(Guid handle, string type, string payload, HttpStatusCode httpStatusCode, string message = null)
        {
            Handle = handle;
            Payload = payload;
            Type = type;
            HttpStatusCode = httpStatusCode;
            Message = message;
        }

        public Guid Handle { get; set; }

        public string Type { get; set; }

        public string Payload { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }

        public string Message { get; set; }

    }

    public class WebHookCreatedEvent : TelemetryEvent
    {
        public WebHookCreatedEvent(string name)
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