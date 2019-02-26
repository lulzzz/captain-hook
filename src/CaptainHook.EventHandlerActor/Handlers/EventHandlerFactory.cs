using System;
using System.Net.Http;
using Autofac.Features.Indexed;
using CaptainHook.Common.Configuration;
using CaptainHook.EventHandlerActor.Handlers.Authentication;
using Eshopworld.Core;

namespace CaptainHook.EventHandlerActor.Handlers
{
    public class EventHandlerFactory : IEventHandlerFactory
    {
        private readonly IIndex<string, HttpClient> _httpClients;
        private readonly IBigBrother _bigBrother;
        private readonly IIndex<string, WebhookConfig> _webHookConfig;
        private readonly IAuthHandlerFactory _authHandlerFactory;

        public EventHandlerFactory(
            IIndex<string, HttpClient> httpClients,
            IBigBrother bigBrother,
            IIndex<string, WebhookConfig> webHookConfig,
            IAuthHandlerFactory authHandlerFactory)
        {
            _httpClients = httpClients;
            _bigBrother = bigBrother;
            _authHandlerFactory = authHandlerFactory;
            _webHookConfig = webHookConfig;
        }

        /// <inheritdoc />
        /// <summary>
        /// Create the custom handler such that we get a mapping from the webhook to the handler selected
        /// </summary>
        /// <param name="eventType"></param>
        /// <returns></returns>
        public IHandler CreateWebhookWithCallbackHandler(string eventType)
        {
            if (!_webHookConfig.TryGetValue(eventType.ToLower(), out var webhookConfig))
            {
                throw new Exception("Boom, handler eventType not found cannot process the message");
            }

            var authHandler = _authHandlerFactory.Get($"{eventType}-webhook");
            if (webhookConfig.CallBackEnabled)
            {
                return new WebhookResponseHandler(
                    this,
                    authHandler,
                    new RequestBuilder(),
                    _bigBrother,
                    _httpClients[webhookConfig.Type.ToLower()],
                    webhookConfig);
            }

            return new GenericWebhookHandler(
                authHandler,
                new RequestBuilder(),
                _bigBrother,
                _httpClients[webhookConfig.Type.ToLower()],
                webhookConfig);
        }

        /// <summary>
        /// Creates a single fire and forget webhook handler
        /// Need this here for now to select the handler for the callback
        /// </summary>
        /// <param name="webHookName"></param>
        /// <returns></returns>
        public IHandler CreateWebhookHandler(string webHookName)
        {
            if (!_webHookConfig.TryGetValue(webHookName.ToLower(), out var webhookConfig))
            {
                throw new Exception("Boom, handler webhook not found cannot process the message");
            }

            var authHandler = _authHandlerFactory.Get(webHookName);

            return new GenericWebhookHandler(
                authHandler,
                new RequestBuilder(),
                _bigBrother,
                _httpClients[webHookName.ToLower()],
                webhookConfig);
        }
    }
}
