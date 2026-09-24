using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Analytics;
using Firebase.Auth;
using Firebase.Crashlytics;
using Firebase.Firestore;
using Firebase.RemoteConfig;
using UnityEngine;

namespace NinjaVillage.Systems.Backend
{
    /// <summary>
    /// Firebase implementation (EPIC 22): anonymous Auth with email linking, Firestore cloud save /
    /// public profile / leaderboard / server time, Remote Config, Analytics and Crashlytics.
    ///
    /// Only selected on Android/iOS device builds (see <see cref="BackendService"/>): the Firebase desktop
    /// native libraries aren't in this repo, so calling Firebase in the Editor would throw. The class is
    /// still compiled on every platform so API mistakes are caught in the Editor.
    ///
    /// Firestore layout (see firebase/firestore.rules):
    /// <c>users/{uid}</c> { save, lastSavedTicks, version, updatedAt, progress fields, displayName }
    /// <c>leaderboard/{uid}</c> { displayName, bestWave, bestKills, updatedAt }
    /// <c>server_time/{uid}</c> { t } — written with a server timestamp and read back for trusted time.
    /// </summary>
    public sealed class FirebaseBackendProvider : IBackendProvider
    {
        private FirebaseAuth _auth;
        private FirebaseFirestore _db;
        private bool _ready;

        public string Name => "Firebase";
        public bool IsOnline => _ready;
        public BackendUser CurrentUser { get; private set; }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                var status = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (status != DependencyStatus.Available)
                {
                    Debug.LogWarning($"[Firebase] Dependencies unavailable: {status}");
                    return false;
                }

                _auth = FirebaseAuth.DefaultInstance;
                _db = FirebaseFirestore.DefaultInstance;
                Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                _ready = true;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Firebase] Init failed: {e.Message}");
                return false;
            }
        }

        // ---------------------------------------------------------------- auth

        public async Task<BackendUser> SignInAnonymouslyAsync()
        {
            if (!_ready) return null;
            try
            {
                var user = _auth.CurrentUser;
                if (user == null)
                {
                    var result = await _auth.SignInAnonymouslyAsync();
                    user = result.User;
                }
                CurrentUser = new BackendUser { UserId = user.UserId, IsAnonymous = user.IsAnonymous, Email = user.Email };
                return CurrentUser;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Firebase] Sign-in failed: {e.Message}");
                return null;
            }
        }

        /// <summary>Upgrades the anonymous account to email/password so progress survives a reinstall.</summary>
        public async Task<bool> LinkEmailAsync(string email, string password)
        {
            if (!_ready || _auth.CurrentUser == null) return false;
            try
            {
                var credential = EmailAuthProvider.GetCredential(email, password);
                var result = await _auth.CurrentUser.LinkWithCredentialAsync(credential);
                CurrentUser = new BackendUser { UserId = result.User.UserId, IsAnonymous = result.User.IsAnonymous, Email = result.User.Email };
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Firebase] Link failed: {e.Message}");
                return false;
            }
        }

        // ---------------------------------------------------------------- cloud save

        public async Task<CloudSaveSnapshot> LoadSaveAsync(string userId)
        {
            if (!_ready || string.IsNullOrEmpty(userId)) return null;
            try
            {
                var snapshot = await _db.Collection("users").Document(userId).GetSnapshotAsync();
                if (!snapshot.Exists || !snapshot.TryGetValue("save", out string json)) return null;

                snapshot.TryGetValue("lastSavedTicks", out long ticks);
                snapshot.TryGetValue("version", out long version);
                snapshot.TryGetValue("totalRuns", out long runs);
                snapshot.TryGetValue("highestWave", out long wave);
                snapshot.TryGetValue("buildingLevels", out long buildings);
                snapshot.TryGetValue("coins", out long coins);
                snapshot.TryGetValue("gems", out long gems);
                return new CloudSaveSnapshot
                {
                    Json = json,
                    LastSavedUtcTicks = ticks,
                    Version = (int)version,
                    Progress = new SaveProgressSummary
                    {
                        TotalRuns = (int)runs, HighestWave = (int)wave, TotalBuildingLevels = (int)buildings, Coins = coins, Gems = gems,
                    },
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Firebase] Load save failed: {e.Message}");
                return null;
            }
        }

        public async Task<bool> WriteSaveAsync(string userId, CloudSaveSnapshot snapshot)
        {
            if (!_ready || string.IsNullOrEmpty(userId) || snapshot == null) return false;
            try
            {
                var data = new Dictionary<string, object>
                {
                    { "save", snapshot.Json },
                    { "lastSavedTicks", snapshot.LastSavedUtcTicks },
                    { "version", snapshot.Version },
                    { "totalRuns", snapshot.Progress.TotalRuns },
                    { "highestWave", snapshot.Progress.HighestWave },
                    { "buildingLevels", snapshot.Progress.TotalBuildingLevels },
                    { "coins", snapshot.Progress.Coins },
                    { "gems", snapshot.Progress.Gems },
                    { "updatedAt", FieldValue.ServerTimestamp },
                };
                await _db.Collection("users").Document(userId).SetAsync(data, SetOptions.MergeAll);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Firebase] Write save failed: {e.Message}");
                return false;
            }
        }

        public async Task<bool> WritePublicProfileAsync(string userId, string displayName, SaveProgressSummary progress)
        {
            if (!_ready || string.IsNullOrEmpty(userId)) return false;
            try
            {
                var data = new Dictionary<string, object>
                {
                    { "displayName", displayName ?? "Ninja" },
                    { "totalRuns", progress.TotalRuns },
                    { "highestWave", progress.HighestWave },
                };
                await _db.Collection("users").Document(userId).SetAsync(data, SetOptions.MergeAll);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Firebase] Write profile failed: {e.Message}");
                return false;
            }
        }

        public async Task<DateTime?> GetServerTimeAsync(string userId)
        {
            if (!_ready || string.IsNullOrEmpty(userId)) return null;
            try
            {
                var doc = _db.Collection("server_time").Document(userId);
                await doc.SetAsync(new Dictionary<string, object> { { "t", FieldValue.ServerTimestamp } });
                var snapshot = await doc.GetSnapshotAsync();
                if (snapshot.TryGetValue("t", out Timestamp timestamp))
                    return timestamp.ToDateTime();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Firebase] Server time failed: {e.Message}");
            }
            return null;
        }

        // ---------------------------------------------------------------- remote config

        public async Task<IDictionary<string, string>> FetchRemoteConfigAsync(IDictionary<string, object> defaults)
        {
            var values = new Dictionary<string, string>();
            if (!_ready) return values;
            try
            {
                var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
                if (defaults != null) await remoteConfig.SetDefaultsAsync(defaults);
                await remoteConfig.FetchAndActivateAsync();
                foreach (var pair in remoteConfig.AllValues)
                    values[pair.Key] = pair.Value.StringValue;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Firebase] Remote Config failed: {e.Message}");
            }
            return values;
        }

        // ---------------------------------------------------------------- analytics / crashlytics

        public void SetAnalyticsEnabled(bool enabled)
        {
            if (_ready) FirebaseAnalytics.SetAnalyticsCollectionEnabled(enabled);
        }

        public void SetUserId(string userId)
        {
            if (!_ready || string.IsNullOrEmpty(userId)) return;
            FirebaseAnalytics.SetUserId(userId);
            Crashlytics.SetUserId(userId);
        }

        public void LogEvent(string name, IReadOnlyList<AnalyticsParam> parameters)
        {
            if (!_ready) return;
            var converted = new Parameter[parameters?.Count ?? 0];
            for (int i = 0; i < converted.Length; i++)
            {
                var p = parameters[i];
                converted[i] = p.Value switch
                {
                    long l => new Parameter(p.Name, l),
                    double d => new Parameter(p.Name, d),
                    _ => new Parameter(p.Name, p.Value?.ToString() ?? string.Empty),
                };
            }
            FirebaseAnalytics.LogEvent(name, converted);
        }

        public void CrashLog(string message)
        {
            if (_ready) Crashlytics.Log(message);
        }

        public void RecordException(Exception exception)
        {
            if (_ready && exception != null) Crashlytics.LogException(exception);
        }

        // ---------------------------------------------------------------- leaderboard

        public async Task<bool> SubmitLeaderboardAsync(LeaderboardEntry entry)
        {
            if (!_ready || entry == null || string.IsNullOrEmpty(entry.UserId)) return false;
            try
            {
                var data = new Dictionary<string, object>
                {
                    { "displayName", entry.DisplayName ?? "Ninja" },
                    { "bestWave", entry.BestWave },
                    { "bestKills", entry.BestKills },
                    { "updatedAt", FieldValue.ServerTimestamp },
                };
                await _db.Collection("leaderboard").Document(entry.UserId).SetAsync(data);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Firebase] Leaderboard submit failed: {e.Message}");
                return false;
            }
        }

        public async Task<List<LeaderboardEntry>> FetchLeaderboardAsync(int count)
        {
            var list = new List<LeaderboardEntry>();
            if (!_ready) return list;
            try
            {
                var query = await _db.Collection("leaderboard")
                    .OrderByDescending("bestWave").OrderByDescending("bestKills").Limit(count).GetSnapshotAsync();
                int rank = 1;
                foreach (var doc in query.Documents)
                {
                    doc.TryGetValue("displayName", out string name);
                    doc.TryGetValue("bestWave", out long wave);
                    doc.TryGetValue("bestKills", out long kills);
                    list.Add(new LeaderboardEntry { UserId = doc.Id, DisplayName = name, BestWave = (int)wave, BestKills = (int)kills, Rank = rank++ });
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Firebase] Leaderboard fetch failed: {e.Message}");
            }
            return list;
        }
    }
}
