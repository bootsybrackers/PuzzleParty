using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PuzzleParty.Service
{
    public interface IBackendSyncService
    {
        bool IsReady { get; }
        event Action OnReady;
        void TrackEvent(string eventType, Dictionary<string, string> data = null);
        Task SyncAsync();
    }
}
