using webapi.Models;

namespace webapi.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByDeviceIdAsync(string deviceId);
        Task UpsertAsync(User user);
    }
}
