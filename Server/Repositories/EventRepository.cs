using Microsoft.Extensions.Options;
using MongoDB.Driver;
using webapi.Configurations;
using webapi.Models;

namespace webapi.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly IMongoCollection<GameEvent> _collection;

        public EventRepository(IMongoClient mongoClient, IOptions<MongoDbSettings> settings)
        {
            var db = mongoClient.GetDatabase(settings.Value.DatabaseName);
            _collection = db.GetCollection<GameEvent>(settings.Value.EventsCollection);
        }

        public async Task InsertManyAsync(IEnumerable<GameEvent> events)
        {
            var list = events.ToList();
            if (list.Count == 0) return;
            await _collection.InsertManyAsync(list);
        }
    }
}
