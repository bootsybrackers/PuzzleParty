using webapi.Models;
using webapi.Repositories;

namespace webapi.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;

        public EventService(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task RecordEventsAsync(string deviceId, string userId, List<GameEvent> events)
        {
            var now = DateTime.UtcNow;
            foreach (var e in events)
            {
                e.Id = null;
                e.DeviceId = deviceId;
                e.UserId = userId;
                e.ServerTimestamp = now;
            }
            await _eventRepository.InsertManyAsync(events);
        }

        public async Task RecordEventAsync(string deviceId, string userId, string eventType, Dictionary<string, string>? data = null)
        {
            var now = DateTime.UtcNow;
            var e = new GameEvent
            {
                DeviceId = deviceId,
                UserId = userId,
                EventType = eventType,
                Data = data ?? new Dictionary<string, string>(),
                ClientTimestamp = now,
                ServerTimestamp = now
            };
            await _eventRepository.InsertManyAsync(new List<GameEvent> { e });
        }
    }
}
