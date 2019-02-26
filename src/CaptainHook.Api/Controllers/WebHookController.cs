using System.Fabric;
using System.Net;
using CaptainHook.Common;
using CaptainHook.Common.Configuration;
using CaptainHook.Interfaces;
using Eshopworld.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace CaptainHook.Api.Controllers
{
    /// <inheritdoc />
    /// <summary>
    /// The Webhook configuration
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class WebhookController : CaptainHookControllerBase
    {
        /// <summary>
        /// 
        /// </summary>
        public WebhookController(
            IHostingEnvironment hostingEnvironment,
            IBigBrother bigBrother,
            StatelessServiceContext sfContext) : base(hostingEnvironment, bigBrother, sfContext)
        {
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
            return Ok(GetActorRef<IMessagingDirector>(Actors.MessageDirector).ReadWebhookAsync(name));
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
            return Ok(GetActorRef<IMessagingDirector>(Actors.MessageDirector).CreateWebhookAsync(config));
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
            return Ok(GetActorRef<IMessagingDirector>(Actors.MessageDirector).UpdateWebhook(config));
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
            GetActorRef<IMessagingDirector>(Actors.MessageDirector).DeleteWebhookAsync(name);
            return NoContent();
        }
    }
}
