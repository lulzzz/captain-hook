using Microsoft.AspNetCore.Mvc;

namespace CaptainHook.Test.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestHookController : ControllerBase
    {
        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut]
        public void Put([FromBody] string value)
        {
        }
    }
}
