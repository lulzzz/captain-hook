using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CaptainHook.Common.Authentication;
using CaptainHook.Common.Configuration;
using CaptainHook.EventHandlerActorService.Handlers;
using CaptainHook.EventHandlerActorService.Handlers.Authentication;
using CaptainHook.Tests.Authentication;
using Eshopworld.Core;
using Eshopworld.Tests.Core;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace CaptainHook.Tests.WebHooks
{
    public class GenericWebhookHandlerTests
    {
        [IsLayer0]
        [Fact]
        public async Task ExecuteHappyPath()
        {
            var (messageData, metaData) = EventHandlerTestHelper.CreateMessageDataPayload();

            var config = new WebhookConfig
            {
                Uri = "http://localhost/webhook",
                HttpVerb = HttpVerb.Put,
                AuthenticationConfig = new AuthenticationConfig(),
                WebhookRequestRules = new List<WebhookRequestRule>
                {
                   new WebhookRequestRule
                   {
                       Source = new ParserLocation
                       {
                           Path = "OrderCode"
                       },
                       Destination = new ParserLocation
                       {
                           Location = Location.Uri
                       }
                   }
                }
            };

            var mockHttp = new MockHttpMessageHandler();
            var webhookRequest = mockHttp.When(HttpMethod.Put, $"{config.Uri}/{metaData["OrderCode"]}")
                .WithContentType("application/json", messageData.Payload)
                .Respond(HttpStatusCode.OK, "application/json", string.Empty);

            var genericWebhookHandler = new GenericWebhookHandler(
                new Mock<IAcquireTokenHandler>().Object,
                new RequestBuilder(),
                new Mock<IBigBrother>().Object,
                mockHttp.ToHttpClient(),
                config);

            await genericWebhookHandler.Call(messageData);

            Assert.Equal(1, mockHttp.GetMatchCount(webhookRequest));
        }
    }
}
