using Microsoft.AspNetCore.Mvc;
using webapi.Models;
using webapi.Services;

namespace webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{deviceId}")]
        public async Task<ActionResult<User>> GetUser(string deviceId)
        {
            var user = await _userService.GetUserAsync(deviceId);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPut("{deviceId}")]
        public async Task<ActionResult<User>> UpsertUser(string deviceId, User user)
        {
            user.DeviceId = deviceId;
            await _userService.UpsertUserAsync(user);
            return Ok(user);
        }
    }
}
