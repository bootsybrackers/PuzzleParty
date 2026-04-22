using webapi.Models;

namespace webapi.Repositories
{
    public interface IEventRepository
    {
        Task InsertManyAsync(IEnumerable<GameEvent> events);
    }
}
