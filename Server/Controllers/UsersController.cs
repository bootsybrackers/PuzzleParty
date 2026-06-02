using Microsoft.AspNetCore.Mvc;
using webapi.Services;

namespace webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("install")]
        public async Task<ActionResult> Install([FromBody] InstallRequest request)
        {
            if (string.IsNullOrEmpty(request.DeviceId))
                return BadRequest("DeviceId is required");

            _logger.LogInformation("Install — deviceId={DeviceId}", request.DeviceId);
            var user = await _userService.InstallAsync(request.DeviceId);
            _logger.LogInformation("Install OK — userId={UserId}", user.UserId);
            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId))
                return BadRequest("UserId is required");

            _logger.LogInformation("Login — userId={UserId}", request.UserId);
            var user = await _userService.LoginAsync(request.UserId);
            if (user == null)
            {
                _logger.LogWarning("Login — userId={UserId} not found", request.UserId);
                return NotFound();
            }
            _logger.LogInformation("Login OK — userId={UserId}, level={Level}", user.UserId, user.LastBeatenLevel);
            return Ok(user);
        }

        [HttpPost("sync")]
        public async Task<ActionResult> Sync([FromBody] SyncRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId))
                return BadRequest("UserId is required");

            _logger.LogInformation("Sync — userId={UserId}, level={Level}, coins={Coins}, streak={Streak}",
                request.UserId, request.LastBeatenLevel, request.Coins, request.Streak);

            var user = await _userService.SyncProgressionAsync(
                request.UserId,
                request.LastBeatenLevel,
                request.Coins,
                request.Streak);

            if (user == null)
            {
                _logger.LogWarning("Sync — userId={UserId} not found", request.UserId);
                return NotFound();
            }
            _logger.LogInformation("Sync OK — userId={UserId}", user.UserId);
            return Ok(user);
        }
    }

    public class InstallRequest
    {
        public string DeviceId { get; set; } = null!;
    }

    public class LoginRequest
    {
        public string UserId { get; set; } = null!;
    }

    public class SyncRequest
    {
        public string UserId { get; set; } = null!;
        public int LastBeatenLevel { get; set; }
        public int Coins { get; set; }
        public int Streak { get; set; }
    }
}
