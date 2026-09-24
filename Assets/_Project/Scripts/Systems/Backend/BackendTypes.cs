using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaVillage.Core.Events;

namespace NinjaVillage.Systems.Backend
{
    public enum BackendStatus
    {
        Offline,
        Connecting,
        Online,
        Error
    }

    /// <summary>Raised whenever the backend connection/sync status changes (Account screen, Home badges).</summary>
    public readonly struct BackendStatusChangedEvent : IGameEvent
    {
        public readonly BackendStatus Status;
        public readonly string Message;
        public BackendStatusChangedEvent(BackendStatus status, string message)
        {
            Status = status;
            Message = message;
        }
    }

    /// <summary>Raised after a successful sign-in.</summary>
    public readonly struct SignedInEvent : IGameEvent
    {
        public readonly string UserId;
        public readonly bool IsAnonymous;
        public SignedInEvent(string userId, bool isAnonymous)
        {
            UserId = userId;
            IsAnonymous = isAnonymous;
        }
    }

    /// <summary>Raised after the local save and the cloud copy were reconciled or uploaded.</summary>
    public readonly struct CloudSaveSyncedEvent : IGameEvent
    {
        public readonly CloudSaveDecision Decision;
        public CloudSaveSyncedEvent(CloudSaveDecision decision) => Decision = decision;
    }

    public sealed class BackendUser
    {
        public string UserId;
        public bool IsAnonymous;
        public string Email;
    }

    /// <summary>The cloud copy of a save: raw JSON plus the metadata needed to reconcile without parsing it.</summary>
    public sealed class CloudSaveSnapshot
    {
        public string Json;
        public long LastSavedUtcTicks;
        public int Version;
        public SaveProgressSummary Progress;
    }

    /// <summary>A few monotonic progress numbers used to judge which save is "further along".</summary>
    [Serializable]
    public struct SaveProgressSummary
    {
        public int TotalRuns;
        public int HighestWave;
        public int TotalBuildingLevels;
        public long Coins;
        public long Gems;

        public bool IsFresh => TotalRuns == 0 && HighestWave == 0 && TotalBuildingLevels <= 1 && Coins == 0 && Gems == 0;

        /// <summary>Single comparable score; runs and waves dominate (they can't be bought).</summary>
        public long Score => TotalRuns * 1_000_000L + HighestWave * 10_000L + TotalBuildingLevels * 100L + Math.Min(Coins / 100, 99);
    }

    public sealed class LeaderboardEntry
    {
        public string UserId;
        public string DisplayName;
        public int BestWave;
        public int BestKills;
        public int Rank; // 1-based when known, 0 otherwise
    }

    /// <summary>One analytics parameter; value is long, double or string.</summary>
    public readonly struct AnalyticsParam
    {
        public readonly string Name;
        public readonly object Value;
        public AnalyticsParam(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }

    /// <summary>
    /// Everything the game needs from a backend. <see cref="FirebaseBackendProvider"/> talks to Firebase on
    /// device builds; <see cref="OfflineBackendProvider"/> keeps the game fully playable in the Editor and
    /// without a network. All async members complete on the main thread when awaited from it.
    /// </summary>
    public interface IBackendProvider
    {
        string Name { get; }
        bool IsOnline { get; }

        Task<bool> InitializeAsync();

        BackendUser CurrentUser { get; }
        Task<BackendUser> SignInAnonymouslyAsync();
        Task<bool> LinkEmailAsync(string email, string password);

        Task<CloudSaveSnapshot> LoadSaveAsync(string userId);
        Task<bool> WriteSaveAsync(string userId, CloudSaveSnapshot snapshot);
        Task<bool> WritePublicProfileAsync(string userId, string displayName, SaveProgressSummary progress);

        /// <summary>Trusted "now" from the server, or null when unavailable.</summary>
        Task<DateTime?> GetServerTimeAsync(string userId);

        Task<IDictionary<string, string>> FetchRemoteConfigAsync(IDictionary<string, object> defaults);

        void SetAnalyticsEnabled(bool enabled);
        void SetUserId(string userId);
        void LogEvent(string name, IReadOnlyList<AnalyticsParam> parameters);

        void CrashLog(string message);
        void RecordException(Exception exception);

        Task<bool> SubmitLeaderboardAsync(LeaderboardEntry entry);
        Task<List<LeaderboardEntry>> FetchLeaderboardAsync(int count);
    }
}
