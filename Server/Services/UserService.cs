using webapi.Models;
using webapi.Repositories;

namespace webapi.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Task<User?> GetUserAsync(string deviceId) =>
            _userRepository.GetByDeviceIdAsync(deviceId);

        public async Task UpsertUserAsync(User user)
        {
            var existing = await _userRepository.GetByDeviceIdAsync(user.DeviceId);
            user.CreatedAt = existing?.CreatedAt ?? DateTime.UtcNow;
            user.LastSyncedAt = DateTime.UtcNow;
            await _userRepository.UpsertAsync(user);
        }
    }
}
