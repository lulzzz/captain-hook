using System;
using System.Net.Http;
using System.Threading.Tasks;
using CaptainHook.Common;
using CaptainHook.Common.Nasty;
using CaptainHook.Common.Telemetry;
using CaptainHook.EventHandlerActor.Handlers.Authentication;
using Eshopworld.Core;
using Newtonsoft.Json;

namespace CaptainHook.EventHandlerActor.Handlers
{
    public class MmEventHandler : GenericEventHandler
    {
        private readonly HttpClient _client;
        private readonly IHandlerFactory _handlerFactory;

        public MmEventHandler(
            IHandlerFactory handlerFactory,
            HttpClient client,
            IBigBrother bigBrother,
            WebHookConfig webHookConfig, 
            IAuthHandler authHandler)
            : base(authHandler, bigBrother, client, webHookConfig)
        {
            _handlerFactory = handlerFactory;
            _client = client;
        }

        public override async Task Call<TRequest>(TRequest request)
        {
            if (!(request is MessageData data))
            {
                throw new Exception("injected wrong implementation");
            }

            if (WebHookConfig.RequiresAuth)
            {
                await AuthHandler.GetToken(_client);
            }

            //todo move order code to body so we don't have to deal with it in CH
            var orderCode = ModelParser.ParseOrderCode(data.Payload);

            var uri = new Uri(new Uri(WebHookConfig.Uri), orderCode.ToString());
            var response = await _client.PostAsJsonReliability(uri.AbsoluteUri, data, BigBrother);

            BigBrother.Publish(new WebhookEvent(data.Handle, data.Type, data.Payload, response.IsSuccessStatusCode.ToString()));

            var eswHandler = _handlerFactory.CreateHandler("esw", "esw");

            var payload = new DispatchHttpResponse
            {
                Content = await response.Content.ReadAsStringAsync(),
                StatusCode = (int) response.StatusCode
            };

            await eswHandler.Call(new MessageData(JsonConvert.SerializeObject(payload), data.Type));
        }
    }
}