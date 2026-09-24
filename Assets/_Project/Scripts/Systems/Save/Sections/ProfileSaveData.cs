using System;
using System.Collections.Generic;
using System.Text;

namespace NinjaVillage.Systems.Save
{
    /// <summary>One finished run, kept for the profile's run history.</summary>
    [Serializable]
    public class RunRecord
    {
        public long EndedUtcTicks;
        public bool Victory;
        public int WaveReached;
        public int Kills;
        public int BossesKilled;
        public int CoinsEarned;
        public float DurationSeconds;
        public string HeroId;
        public string WeaponId;
        /// <summary>Chapter played (empty for runs recorded before chapters existed).</summary>
        public string ChapterId;
    }

    /// <summary>Player identity + lifetime stats (EPIC 16 "Player profile").</summary>
    [Serializable]
    public class ProfileSaveData
    {
        public const int MaxRunHistory = 20;
        public const int MinDisplayNameLength = 3;
        public const int MaxDisplayNameLength = 16;
        public const string DefaultDisplayName = "Ninja";

        /// <summary>Local GUID until an auth backend links a real account id.</summary>
        public string PlayerId;
        public string DisplayName = DefaultDisplayName;
        public long CreatedUtcTicks;
        public long LastSessionUtcTicks;

        public int TotalRuns;
        public int TotalVictories;
        public int TotalKills;
        public int TotalBossesKilled;
        public long TotalCoinsEarned;
        public int HighestWaveReached;
        public float LongestRunSeconds;

        public List<RunRecord> RecentRuns = new();

        /// <summary>0..1 share of runs that ended in victory.</summary>
        public float WinRate => TotalRuns > 0 ? (float)TotalVictories / TotalRuns : 0f;

        public void EnsureInitialized()
        {
            if (string.IsNullOrEmpty(PlayerId))
                PlayerId = Guid.NewGuid().ToString("N");
            if (CreatedUtcTicks == 0)
                CreatedUtcTicks = DateTime.UtcNow.Ticks;
            if (string.IsNullOrWhiteSpace(DisplayName))
                DisplayName = DefaultDisplayName;
            RecentRuns ??= new List<RunRecord>();
        }

        /// <summary>Folds a finished run into lifetime stats and the capped history list.</summary>
        public void RecordRun(RunRecord run)
        {
            if (run == null) return;

            TotalRuns++;
            if (run.Victory) TotalVictories++;
            TotalKills += run.Kills;
            TotalBossesKilled += run.BossesKilled;
            TotalCoinsEarned += run.CoinsEarned;
            if (run.WaveReached > HighestWaveReached) HighestWaveReached = run.WaveReached;
            if (run.DurationSeconds > LongestRunSeconds) LongestRunSeconds = run.DurationSeconds;

            RecentRuns.Insert(0, run);
            if (RecentRuns.Count > MaxRunHistory)
                RecentRuns.RemoveRange(MaxRunHistory, RecentRuns.Count - MaxRunHistory);
        }

        /// <summary>Validates and applies a new display name. Returns false (and an error message) if rejected.</summary>
        public bool TrySetDisplayName(string raw, out string error)
        {
            if (!TrySanitizeDisplayName(raw, out string clean, out error)) return false;
            DisplayName = clean;
            return true;
        }

        /// <summary>
        /// Display-name rules: letters, digits, spaces, '_' '-' '.' only (so the name can't inject
        /// TextMeshPro rich-text tags into leaderboards/UI), runs of spaces collapsed, trimmed,
        /// <see cref="MinDisplayNameLength"/>..<see cref="MaxDisplayNameLength"/> characters.
        /// </summary>
        public static bool TrySanitizeDisplayName(string raw, out string clean, out string error)
        {
            clean = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "Name can't be empty.";
                return false;
            }

            var builder = new StringBuilder(raw.Length);
            bool lastWasSpace = false;
            foreach (char c in raw.Trim())
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!lastWasSpace) builder.Append(' ');
                    lastWasSpace = true;
                    continue;
                }

                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.')
                {
                    error = $"'{c}' isn't allowed in names.";
                    return false;
                }
                builder.Append(c);
                lastWasSpace = false;
            }

            string result = builder.ToString();
            if (result.Length < MinDisplayNameLength)
            {
                error = $"Name needs at least {MinDisplayNameLength} characters.";
                return false;
            }
            if (result.Length > MaxDisplayNameLength)
            {
                error = $"Name can have at most {MaxDisplayNameLength} characters.";
                return false;
            }

            clean = result;
            error = null;
            return true;
        }
    }
}
