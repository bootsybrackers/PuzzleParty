using System.Collections.Generic;
using System.Threading.Tasks;

namespace PuzzleParty.Service
{
    public interface IBackendSyncService
    {
        void TrackEvent(string eventType, Dictionary<string, string> data = null);
        Task SyncAsync();
    }
}
