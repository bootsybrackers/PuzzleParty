using Microsoft.AspNetCore.Mvc;
using webapi.Models;
using webapi.Services;

namespace webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpPost("batch")]
        public async Task<ActionResult> PostBatch(BatchEventsRequest request)
        {
            if (string.IsNullOrEmpty(request.DeviceId))
                return BadRequest("DeviceId is required");

            await _eventService.RecordEventsAsync(request.DeviceId, request.Events);
            return Ok(new { inserted = request.Events.Count });
        }
    }
}
