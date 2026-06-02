using Microsoft.AspNetCore.Mvc;
using webapi.Models;
using webapi.Services;

namespace webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrackingController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ILogger<TrackingController> _logger;

        public TrackingController(IEventService eventService, ILogger<TrackingController> logger)
        {
            _eventService = eventService;
            _logger = logger;
        }

        [HttpPost("batch")]
        public async Task<ActionResult> PostBatch(BatchEventsRequest request)
        {
            if (string.IsNullOrEmpty(request.DeviceId))
                return BadRequest("DeviceId is required");
            if (string.IsNullOrEmpty(request.UserId))
                return BadRequest("UserId is required");

            _logger.LogInformation("Tracking batch — userId={UserId}, count={Count}",
                request.UserId, request.Events.Count);

            await _eventService.RecordEventsAsync(request.DeviceId, request.UserId, request.Events);

            _logger.LogInformation("Tracking batch OK — inserted {Count} events", request.Events.Count);
            return Ok(new { inserted = request.Events.Count });
        }
    }
}
