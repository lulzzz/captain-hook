using System.Net;

namespace CaptainHook.Common.Telemetry
{
    using System;

    public class HttpClientFailure : WebhookEvent
    {
        public HttpClientFailure(Guid handle, string type, string payload, HttpStatusCode httpStatusCode, string message = null)
            : base(handle, type, payload, httpStatusCode, message)
        {

        }
    }
}