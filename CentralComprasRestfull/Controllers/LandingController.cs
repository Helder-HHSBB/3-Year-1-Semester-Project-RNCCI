using Microsoft.AspNetCore.Mvc;

namespace CentralComprasRestfull.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LandingController : Controller
    {
        [HttpGet]
        [ProducesResponseType(200)]
        public ActionResult<string> Get()
        {
            return Ok("Everything is gooooddddd in Central Compras Estado ");
        }
    }
}
