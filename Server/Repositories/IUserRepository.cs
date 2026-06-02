using webapi.Models;

namespace webapi.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByUserIdAsync(string userId);
        Task<User> CreateAsync(User user);
        Task UpdateProgressionAsync(string userId, int lastBeatenLevel, int coins, int streak);
    }
}
