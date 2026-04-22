using webapi.Models;

namespace webapi.Services
{
    public interface IEventService
    {
        Task RecordEventsAsync(string deviceId, List<GameEvent> events);
    }
}
