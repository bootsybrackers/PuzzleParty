using Microsoft.AspNetCore.Mvc;

namespace webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public ActionResult Get()
        {
            return Ok(new
            {
                status = "ok",
                version = Environment.GetEnvironmentVariable("APP_VERSION") ?? "unknown",
                serverTime = DateTime.UtcNow
            });
        }
    }
}
