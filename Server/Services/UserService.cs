using webapi.Models;
using webapi.Repositories;

namespace webapi.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEventService _eventService;

        public UserService(IUserRepository userRepository, IEventService eventService)
        {
            _userRepository = userRepository;
            _eventService = eventService;
        }

        public async Task<User> InstallAsync(string deviceId)
        {
            var user = new User
            {
                UserId = Guid.NewGuid().ToString(),
                DeviceId = deviceId,
                CreatedAt = DateTime.UtcNow,
                LastSyncedAt = DateTime.UtcNow
            };
            await _userRepository.CreateAsync(user);
            await _eventService.RecordEventAsync(deviceId, user.UserId, "app_install");
            return user;
        }

        public async Task<User?> LoginAsync(string userId)
        {
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user == null) return null;
            await _eventService.RecordEventAsync(user.DeviceId, userId, "app_start");
            return user;
        }

        public async Task<User?> SyncProgressionAsync(string userId, int lastBeatenLevel, int coins, int streak)
        {
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user == null) return null;
            await _userRepository.UpdateProgressionAsync(userId, lastBeatenLevel, coins, streak);
            user.LastBeatenLevel = lastBeatenLevel;
            user.Coins = coins;
            user.Streak = streak;
            return user;
        }
    }
}
