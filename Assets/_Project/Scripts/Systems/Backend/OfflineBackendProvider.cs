using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Backend
{
    /// <summary>
    /// Backend used in the Editor and whenever Firebase is unavailable: the local save is the only copy,
    /// the device clock is "server time", Remote Config returns its defaults, analytics are printed
    /// (when verbose) instead of sent, and the leaderboard shows only the local player's own best run.
    /// Published villages are kept in memory for the session, so visiting can be tried in the Editor.
    /// The game stays fully playable; screens show an "Offline" state.
    /// </summary>
    public sealed class OfflineBackendProvider : IBackendProvider
    {
        public bool VerboseAnalytics;

        private readonly Dictionary<string, PublicVillage> _villages = new();

        public string Name => "Offline";
        public bool IsOnline => false;
        public BackendUser CurrentUser { get; private set; }

        public Task<bool> InitializeAsync() => Task.FromResult(true);

        public Task<BackendUser> SignInAnonymouslyAsync()
        {
            CurrentUser = new BackendUser { UserId = SaveService.Data.Profile.PlayerId, IsAnonymous = true };
            return Task.FromResult(CurrentUser);
        }

        public Task<bool> LinkEmailAsync(string email, string password) => Task.FromResult(false);

        public Task<CloudSaveSnapshot> LoadSaveAsync(string userId) => Task.FromResult<CloudSaveSnapshot>(null);
        public Task<bool> WriteSaveAsync(string userId, CloudSaveSnapshot snapshot) => Task.FromResult(false);
        public Task<bool> WritePublicProfileAsync(string userId, string displayName, SaveProgressSummary progress) => Task.FromResult(false);

        public Task<DateTime?> GetServerTimeAsync(string userId) => Task.FromResult<DateTime?>(null);

        public Task<IDictionary<string, string>> FetchRemoteConfigAsync(IDictionary<string, object> defaults)
        {
            var values = new Dictionary<string, string>();
            if (defaults != null)
                foreach (var pair in defaults)
                    values[pair.Key] = Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture);
            return Task.FromResult<IDictionary<string, string>>(values);
        }

        public void SetAnalyticsEnabled(bool enabled) { }
        public void SetUserId(string userId) { }

        public void LogEvent(string name, IReadOnlyList<AnalyticsParam> parameters)
        {
            if (!VerboseAnalytics) return;
            var parts = new List<string>();
            if (parameters != null) foreach (var p in parameters) parts.Add($"{p.Name}={p.Value}");
            Debug.Log($"[Analytics:offline] {name} {string.Join(", ", parts)}");
        }

        public void CrashLog(string message) { }
        public void RecordException(Exception exception) { }

        public Task<bool> SubmitLeaderboardAsync(LeaderboardEntry entry) => Task.FromResult(false);

        public Task<bool> WriteVillageAsync(PublicVillage village)
        {
            if (village == null || string.IsNullOrEmpty(village.UserId)) return Task.FromResult(false);
            village.UpdatedUtc = DateTime.UtcNow;
            _villages[village.UserId] = village;
            return Task.FromResult(true);
        }

        public Task<PublicVillage> LoadVillageAsync(string userId) =>
            Task.FromResult(userId != null && _villages.TryGetValue(userId, out var village) ? village : null);

        public Task<List<PublicVillage>> ListVillagesAsync(int count)
        {
            var list = new List<PublicVillage>(_villages.Values);
            list.Sort((a, b) => Nullable.Compare(b.UpdatedUtc, a.UpdatedUtc));
            if (list.Count > count) list.RemoveRange(count, list.Count - count);
            return Task.FromResult(list);
        }

        public Task<List<LeaderboardEntry>> FetchLeaderboardAsync(int count)
        {
            var profile = SaveService.Data.Profile;
            var list = new List<LeaderboardEntry>();
            if (profile.HighestWaveReached > 0)
            {
                list.Add(new LeaderboardEntry
                {
                    UserId = profile.PlayerId,
                    DisplayName = profile.DisplayName,
                    BestWave = profile.HighestWaveReached,
                    BestKills = profile.TotalKills,
                    Rank = 1,
                });
            }
            return Task.FromResult(list);
        }
    }
}
