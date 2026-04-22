using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace webapi.Models
{
    public class GameEvent
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string DeviceId { get; set; } = null!;
        public string EventType { get; set; } = null!;
        public Dictionary<string, string> Data { get; set; } = new();
        public DateTime ClientTimestamp { get; set; }
        public DateTime ServerTimestamp { get; set; }
    }

    public class BatchEventsRequest
    {
        public string DeviceId { get; set; } = null!;
        public List<GameEvent> Events { get; set; } = new();
    }
}
