using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PuzzleParty.Progressions;
using UnityEngine;

namespace PuzzleParty.Service
{
    public class BackendSyncService : IBackendSyncService
    {
        // TODO: change to your production URL before release
        private const string BaseUrl = "http://localhost:5136";
        private const float SyncIntervalMinutes = 5f;

        private static readonly HttpClient Http = new();

        private readonly string _deviceId;
        private readonly IProgressionService _progressionService;
        private readonly List<PendingEvent> _pendingEvents = new();

        public BackendSyncService(IProgressionService progressionService)
        {
            _progressionService = progressionService;
            _deviceId = GetOrCreateDeviceId();

            Application.quitting += () => _ = SyncAsync();
            Application.focusChanged += hasFocus => { if (!hasFocus) _ = SyncAsync(); };

            _ = RunSyncLoopAsync();
        }

        public void TrackEvent(string eventType, Dictionary<string, string> data = null)
        {
            _pendingEvents.Add(new PendingEvent
            {
                eventType = eventType,
                data = data ?? new Dictionary<string, string>(),
                clientTimestamp = DateTime.UtcNow.ToString("o")
            });
        }

        public async Task SyncAsync()
        {
            try
            {
                await SyncUserAsync();
                await SyncEventsAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BackendSync] Sync failed, will retry next cycle: {ex.Message}");
            }
        }

        private async Task RunSyncLoopAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(SyncIntervalMinutes));
                await SyncAsync();
            }
        }

        private async Task SyncUserAsync()
        {
            var progression = _progressionService.GetProgression();
            var payload = JsonConvert.SerializeObject(new
            {
                lastBeatenLevel = progression.lastBeatenLevel,
                coins = progression.coins,
                streak = progression.streak
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            await Http.PutAsync($"{BaseUrl}/api/users/{_deviceId}", content);
        }

        private async Task SyncEventsAsync()
        {
            if (_pendingEvents.Count == 0) return;

            var snapshot = new List<PendingEvent>(_pendingEvents);
            var payload = JsonConvert.SerializeObject(new
            {
                deviceId = _deviceId,
                events = snapshot
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await Http.PostAsync($"{BaseUrl}/api/events/batch", content);

            if (response.IsSuccessStatusCode)
            {
                _pendingEvents.RemoveAll(e => snapshot.Contains(e));
                Debug.Log($"[BackendSync] Synced {snapshot.Count} events");
            }
        }

        private static string GetOrCreateDeviceId()
        {
            string id = PlayerPrefs.GetString("DeviceId", "");
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
                PlayerPrefs.SetString("DeviceId", id);
                PlayerPrefs.Save();
            }
            return id;
        }

        [Serializable]
        private class PendingEvent
        {
            public string eventType;
            public Dictionary<string, string> data;
            public string clientTimestamp;
        }
    }
}
