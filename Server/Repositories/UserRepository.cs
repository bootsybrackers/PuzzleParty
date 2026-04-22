using Microsoft.Extensions.Options;
using MongoDB.Driver;
using webapi.Configurations;
using webapi.Models;

namespace webapi.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<User> _collection;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(IMongoClient mongoClient, IOptions<MongoDbSettings> settings, ILogger<UserRepository> logger)
        {
            var db = mongoClient.GetDatabase(settings.Value.DatabaseName);
            _collection = db.GetCollection<User>(settings.Value.UsersCollection);
            _logger = logger;
        }

        public async Task<User?> GetByDeviceIdAsync(string deviceId)
        {
            return await _collection.Find(u => u.DeviceId == deviceId).FirstOrDefaultAsync();
        }

        public async Task UpsertAsync(User user)
        {
            var filter = Builders<User>.Filter.Eq(u => u.DeviceId, user.DeviceId);
            var options = new ReplaceOptions { IsUpsert = true };
            await _collection.ReplaceOneAsync(filter, user, options);
            _logger.LogDebug("Upserted user {DeviceId}", user.DeviceId);
        }
    }
}
