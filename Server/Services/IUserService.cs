using webapi.Models;

namespace webapi.Services
{
    public interface IUserService
    {
        Task<User?> GetUserAsync(string deviceId);
        Task UpsertUserAsync(User user);
    }
}
