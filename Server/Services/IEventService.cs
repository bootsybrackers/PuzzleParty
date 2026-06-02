using webapi.Models;

namespace webapi.Services
{
    public interface IEventService
    {
        Task RecordEventsAsync(string deviceId, string userId, List<GameEvent> events);
        Task RecordEventAsync(string deviceId, string userId, string eventType, Dictionary<string, string>? data = null);
    }
}
