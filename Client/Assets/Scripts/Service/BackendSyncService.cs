using System;
using System.Collections.Generic;
using System.Net;
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
        private const float SyncIntervalMinutes = 10f;

        private static readonly HttpClient Http = new();

        private readonly string _deviceId;
        private readonly IProgressionService _progressionService;
        private readonly List<PendingEvent> _pendingEvents = new();
        private string _userId = "";
        private bool _isInitialized = false;

        public bool IsReady { get; private set; } = false;
        public event Action OnReady;

        public BackendSyncService(IProgressionService progressionService)
        {
            _progressionService = progressionService;
            _deviceId = GetOrCreateDeviceId();

            Application.quitting += () => _ = SyncAsync();
            Application.focusChanged += hasFocus => { if (!hasFocus) _ = SyncAsync(); };

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            _userId = PlayerPrefs.GetString("UserId", "");
            Debug.Log($"[BackendSync] Initializing — deviceId={_deviceId}, savedUserId={(string.IsNullOrEmpty(_userId) ? "<none>" : _userId)}");
            try
            {
                if (string.IsNullOrEmpty(_userId))
                    await InstallAsync();
                else
                    await LoginAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BackendSync] Initialization failed: {ex.Message}");
            }

            _isInitialized = true;
            IsReady = true;
            Debug.Log($"[BackendSync] Init complete — userId={(string.IsNullOrEmpty(_userId) ? "<not set>" : _userId)}, starting sync loop");
            OnReady?.Invoke();
            _ = RunSyncLoopAsync();
        }

        private async Task InstallAsync()
        {
            Debug.Log($"[BackendSync] Installing new user for deviceId={_deviceId}");
            var payload = JsonConvert.SerializeObject(new { deviceId = _deviceId });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await Http.PostAsync($"{BaseUrl}/api/users/install", content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                Debug.LogWarning($"[BackendSync] Install failed: {response.StatusCode} — {body}");
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<UserResponse>(json);
            _userId = user.userId;
            PlayerPrefs.SetString("UserId", _userId);
            PlayerPrefs.Save();

            ApplyServerProgression(user);
            Debug.Log($"[BackendSync] Install OK — userId={_userId}, progression=({user.lastBeatenLevel}/{user.coins}/{user.streak})");
        }

        private async Task LoginAsync()
        {
            Debug.Log($"[BackendSync] Logging in userId={_userId}");
            var payload = JsonConvert.SerializeObject(new { userId = _userId });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await Http.PostAsync($"{BaseUrl}/api/users/login", content);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Debug.LogWarning($"[BackendSync] Login 404 — userId unknown on server, re-installing");
                PlayerPrefs.DeleteKey("UserId");
                PlayerPrefs.Save();
                _userId = "";
                await InstallAsync();
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                Debug.LogWarning($"[BackendSync] Login failed: {response.StatusCode} — {body}");
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<UserResponse>(json);
            ApplyServerProgression(user);
            Debug.Log($"[BackendSync] Login OK — userId={_userId}, progression=({user.lastBeatenLevel}/{user.coins}/{user.streak})");
        }

        private void ApplyServerProgression(UserResponse user)
        {
            var progression = _progressionService.GetProgression();
            progression.lastBeatenLevel = user.lastBeatenLevel;
            progression.coins = user.coins;
            progression.streak = user.streak;
            _progressionService.SaveProgression(progression);
        }

        public void TrackEvent(string eventType, Dictionary<string, string> data = null)
        {
            _pendingEvents.Add(new PendingEvent
            {
                eventType = eventType,
                data = data ?? new Dictionary<string, string>(),
                clientTimestamp = DateTime.UtcNow.ToString("o")
            });
            Debug.Log($"[BackendSync] Tracked '{eventType}' — {_pendingEvents.Count} event(s) pending");
        }

        public async Task SyncAsync()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[BackendSync] SyncAsync called before init completed — skipping");
                return;
            }
            if (string.IsNullOrEmpty(_userId))
            {
                Debug.LogWarning("[BackendSync] SyncAsync called but userId is not set — skipping");
                return;
            }
            Debug.Log($"[BackendSync] Syncing — userId={_userId}, pendingEvents={_pendingEvents.Count}");
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
                Debug.Log("[BackendSync] Periodic sync loop firing");
                await SyncAsync();
            }
        }

        private async Task SyncUserAsync()
        {
            var progression = _progressionService.GetProgression();
            Debug.Log($"[BackendSync] Syncing progression — level={progression.lastBeatenLevel}, coins={progression.coins}, streak={progression.streak}");
            var payload = JsonConvert.SerializeObject(new
            {
                userId = _userId,
                lastBeatenLevel = progression.lastBeatenLevel,
                coins = progression.coins,
                streak = progression.streak
            });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await Http.PostAsync($"{BaseUrl}/api/users/sync", content);
            if (response.IsSuccessStatusCode)
                Debug.Log("[BackendSync] Progression sync OK");
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                Debug.LogWarning($"[BackendSync] Progression sync failed: {response.StatusCode} — {body}");
            }
        }

        private async Task SyncEventsAsync()
        {
            if (_pendingEvents.Count == 0) return;

            var snapshot = new List<PendingEvent>(_pendingEvents);
            var payload = JsonConvert.SerializeObject(new
            {
                deviceId = _deviceId,
                userId = _userId,
                events = snapshot
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await Http.PostAsync($"{BaseUrl}/api/tracking/batch", content);

            if (response.IsSuccessStatusCode)
            {
                _pendingEvents.RemoveAll(e => snapshot.Contains(e));
                Debug.Log($"[BackendSync] Events sync OK — sent {snapshot.Count}, {_pendingEvents.Count} remaining");
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                Debug.LogWarning($"[BackendSync] Events sync failed: {response.StatusCode} — {body}");
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

        [Serializable]
        private class UserResponse
        {
            public string userId;
            public int lastBeatenLevel;
            public int coins;
            public int streak;
        }
    }
}
