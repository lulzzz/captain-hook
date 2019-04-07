using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaptainHook.Api.Controllers
{
    /// <summary>
    /// health probe controller
    /// </summary>
    ///
    [ApiVersionNeutral]
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [AllowAnonymous]
    public class ProbeController : ControllerBase
    {
        /// <summary>
        /// empty get to serve as health probe endpoint
        /// </summary>
        [HttpGet]
        public void Get()
        {

        }
    }
}
