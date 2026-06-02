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

        public async Task<User?> GetByUserIdAsync(string userId)
        {
            return await _collection.Find(u => u.UserId == userId).FirstOrDefaultAsync();
        }

        public async Task<User> CreateAsync(User user)
        {
            await _collection.InsertOneAsync(user);
            _logger.LogDebug("Created user {UserId} for device {DeviceId}", user.UserId, user.DeviceId);
            return user;
        }

        public async Task UpdateProgressionAsync(string userId, int lastBeatenLevel, int coins, int streak)
        {
            var filter = Builders<User>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<User>.Update
                .Set(u => u.LastBeatenLevel, lastBeatenLevel)
                .Set(u => u.Coins, coins)
                .Set(u => u.Streak, streak)
                .Set(u => u.LastSyncedAt, DateTime.UtcNow);
            await _collection.UpdateOneAsync(filter, update);
            _logger.LogInformation("Updated progression for user {UserId} — level={Level}, coins={Coins}, streak={Streak}",
                userId, lastBeatenLevel, coins, streak);
        }
    }
}
