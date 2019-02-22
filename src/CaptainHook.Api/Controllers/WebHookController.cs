using System;
using System.Net;
using CaptainHook.Common.Configuration;
using CaptainHook.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace CaptainHook.Api.Controllers
{
    /// <summary>
    /// The Webhook configuration
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class WebHookController : Controller
    {
        private readonly IMessagingDirector _messagingDirectorActor;

        /// <summary>
        /// 
        /// </summary>
        public WebHookController()
        {
            var serviceUri = new Uri("fabric:/CaptainHook/MessagingDirectorActor");
            _messagingDirectorActor = ActorProxy.Create<IMessagingDirector>(new ActorId(0), serviceUri);
        }

        //todo get paginated list

        /// <summary>
        /// Get a webhook configuration
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(WebhookConfig), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult Get(string name)
        {
            return Ok(_messagingDirectorActor.ReadWebhook(name));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(WebhookConfig), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string[]), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult Post(WebhookConfig config)
        {
            config = _messagingDirectorActor.CreateWebhook(config);
            return Ok(config);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [HttpPut]
        [ProducesResponseType(typeof(string[]), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(string[]), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult Put(WebhookConfig config)
        {
            config = _messagingDirectorActor.UpdateWebhook(config);
            return Ok(config);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult Delete(string name)
        {
            _messagingDirectorActor.DeleteWebhook(name);
            return NoContent();
        }
    }
}
