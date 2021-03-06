﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CaptainHook.Common;
using CaptainHook.Common.Authentication;
using CaptainHook.Common.Configuration;
using CaptainHook.Common.Telemetry;
using CaptainHook.EventHandlerActor.Handlers.Authentication;
using Eshopworld.Core;

namespace CaptainHook.EventHandlerActor.Handlers
{
    public class WebhookResponseHandler : GenericWebhookHandler
    {
        private readonly HttpClient _client;
        private readonly EventHandlerConfig _eventHandlerConfig;
        private readonly IEventHandlerFactory _eventHandlerFactory;

        public WebhookResponseHandler(
            IEventHandlerFactory eventHandlerFactory,
            IAcquireTokenHandler acquireTokenHandler,
            IRequestBuilder requestBuilder,
            IBigBrother bigBrother,
            HttpClient client,
            EventHandlerConfig eventHandlerConfig)
            : base(acquireTokenHandler, requestBuilder, bigBrother, client, eventHandlerConfig.WebHookConfig)
        {
            _eventHandlerFactory = eventHandlerFactory;
            _client = client;
            _eventHandlerConfig = eventHandlerConfig;
        }

        public override async Task Call<TRequest>(TRequest request, IDictionary<string, object> metadata = null)
        {
            if (!(request is MessageData messageData))
            {
                throw new Exception("injected wrong implementation");
            }

            if (WebhookConfig.AuthenticationConfig.Type != AuthenticationType.None)
            {
                await AcquireTokenHandler.GetToken(_client);
            }

            var uri = RequestBuilder.BuildUri(WebhookConfig, messageData.Payload);
            var httpVerb = RequestBuilder.SelectHttpVerb(WebhookConfig, messageData.Payload);
            var payload = RequestBuilder.BuildPayload(WebhookConfig, messageData.Payload, metadata);

            void TelemetryEvent(string msg)
            {
                BigBrother.Publish(new HttpClientFailure(messageData.Handle, messageData.Type, messageData.Payload, msg));
            }

            var response = await _client.ExecuteAsJsonReliably(httpVerb, uri, payload, TelemetryEvent);

            BigBrother.Publish(new WebhookEvent(messageData.Handle, messageData.Type, messageData.Payload, response.IsSuccessStatusCode.ToString()));

            if (metadata == null)
            {
                metadata = new Dictionary<string, object>();
            }
            else
            {
                metadata.Clear();
            }

            var content = await response.Content.ReadAsStringAsync();
            metadata.Add("HttpStatusCode", (int)response.StatusCode);
            metadata.Add("HttpResponseContent", content);

            BigBrother.Publish(new WebhookEvent(messageData.Handle, messageData.Type, content));

            //call callback
            var eswHandler = _eventHandlerFactory.CreateWebhookHandler(_eventHandlerConfig.CallbackConfig.Name);

            await eswHandler.Call(messageData, metadata);
        }
    }
}
