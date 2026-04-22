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

        public async Task RecordEventsAsync(string deviceId, List<GameEvent> events)
        {
            var now = DateTime.UtcNow;
            foreach (var e in events)
            {
                e.Id = null; // let MongoDB generate the ID
                e.DeviceId = deviceId;
                e.ServerTimestamp = now;
            }

            await _eventRepository.InsertManyAsync(events);
        }
    }
}
