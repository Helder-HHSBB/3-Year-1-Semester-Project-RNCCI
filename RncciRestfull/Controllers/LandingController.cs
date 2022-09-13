using Microsoft.AspNetCore.Mvc;

namespace RncciRestfull.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LandingController : Controller
    {
        [HttpGet]
        [ProducesResponseType(200)]
        public ActionResult<string> Get()
        {
            return Ok("Everything is gooooddddd.... ");
        }
    }
}
