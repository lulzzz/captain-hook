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
        private readonly IAuthHandlerFactory _authHandlerFactory;

        public EventHandlerFactory(
            IIndex<string, HttpClient> httpClients,
            IBigBrother bigBrother,
            IAuthHandlerFactory authHandlerFactory)
        {
            _httpClients = httpClients;
            _bigBrother = bigBrother;
            _authHandlerFactory = authHandlerFactory;
        }

        /// <summary>
        /// Create the custom handler such that we get a mapping from the webhook to the handler selected
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="webhookConfig"></param>
        /// <returns></returns>
        public IHandler CreateWebhookWithCallbackHandler(string eventType, WebhookConfig webhookConfig)
        {
            if (webhookConfig == null)
            {
                throw new ArgumentNullException(nameof(webhookConfig), "Cannot be null");
            }

            var authHandler = _authHandlerFactory.Get(webhookConfig.AuthenticationConfig);
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
        /// <param name="webhookConfig"></param>
        /// <returns></returns>
        public IHandler CreateWebhookHandler(WebhookConfig webhookConfig)
        {
            if (webhookConfig == null)
            {
                throw new ArgumentNullException(nameof(webhookConfig), "Cannot be null");
            }

            var authHandler = _authHandlerFactory.Get(webhookConfig.AuthenticationConfig);

            return new GenericWebhookHandler(
                authHandler,
                new RequestBuilder(),
                _bigBrother,
                _httpClients[webhookConfig.Type.ToLower()],
                webhookConfig);
        }
    }
}
