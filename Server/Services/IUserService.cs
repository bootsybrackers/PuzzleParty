using webapi.Models;

namespace webapi.Services
{
    public interface IUserService
    {
        Task<User> InstallAsync(string deviceId);
        Task<User?> LoginAsync(string userId);
        Task<User?> SyncProgressionAsync(string userId, int lastBeatenLevel, int coins, int streak);
    }
}
