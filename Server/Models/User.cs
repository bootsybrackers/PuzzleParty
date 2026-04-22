using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace webapi.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string DeviceId { get; set; } = null!;

        public int LastBeatenLevel { get; set; }
        public int Coins { get; set; }
        public int Streak { get; set; }

        public DateTime LastSyncedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
