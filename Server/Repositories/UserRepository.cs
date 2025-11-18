using webapi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace webapi.Repositories
{
    public class UserRepository : IUserRepository
    {
        private static readonly List<User> _users = new();
        private readonly ILogger<UserRepository> logger;

        public UserRepository(ILogger<UserRepository> logger)
        {
            this.logger = logger;
        }

        public Task<IEnumerable<User>> GetAllAsync()
        {
            logger.LogDebug("Fetching all users from list. no: "+_users.Count);
            return Task.FromResult<IEnumerable<User>>(_users);
        }

        public Task<User?> GetByIdAsync(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            return Task.FromResult(user);
        }

        public Task AddAsync(User user)
        {
            user.Id = _users.Count + 1; // Simple auto-increment
            _users.Add(user);
            logger.LogDebug("Added new user to repo. name:"+user.Name+" email:"+user.Email+" repo count:"+_users.Count);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user)
        {
            var existing = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existing != null)
            {
                existing.Name = user.Name;
                existing.Email = user.Email;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                _users.Remove(user);
            }
            return Task.CompletedTask;
        }
    }
}
