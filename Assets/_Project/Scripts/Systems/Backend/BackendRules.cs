using System;
using System.Collections.Generic;
using System.Text;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Backend
{
    public enum CloudSaveDecision
    {
        /// <summary>No cloud copy, or local is newer/further — upload local.</summary>
        UseLocal,
        /// <summary>Adopt the cloud copy (replaces the local save).</summary>
        UseCloud,
        /// <summary>Both copies are the same save.</summary>
        InSync
    }

    /// <summary>
    /// Backend rules with no Unity/Firebase dependencies (EditMode-tested).
    /// </summary>
    public static class BackendRules
    {
        // ---------------------------------------------------------------- cloud save

        /// <summary>
        /// What other players see of <paramref name="save"/>'s village: headline numbers plus the full
        /// <see cref="Village.VillageSnapshot"/> JSON (buildings, decorations, heroes, pets, gear, talents).
        /// </summary>
        public static PublicVillage BuildPublicVillage(SaveData save, string userId)
        {
            var snapshot = Village.VillageSnapshot.FromSave(save);
            snapshot.PlayerId = userId;
            snapshot.TakenUtcTicks = 0; // the server's updatedAt says when; this keeps unchanged villages byte-identical
            return new PublicVillage
            {
                UserId = userId,
                DisplayName = snapshot.DisplayName,
                CastleLevel = Math.Max(1, snapshot.BuildingLevel(BuildingIds.Castle)),
                HighestWave = snapshot.HighestWave,
                ChaptersCleared = snapshot.ChaptersCleared,
                AchievementTiers = snapshot.AchievementTiers,
                SnapshotJson = UnityEngine.JsonUtility.ToJson(snapshot),
            };
        }

        public static SaveProgressSummary Summarize(SaveData save)
        {
            if (save == null) return default;
            return new SaveProgressSummary
            {
                TotalRuns = save.Profile != null ? save.Profile.TotalRuns : 0,
                HighestWave = save.Profile != null ? save.Profile.HighestWaveReached : 0,
                TotalBuildingLevels = save.Village != null ? save.Village.TotalBuildingLevels() : 0,
                Coins = save.Wallet != null ? save.Wallet.Coins : 0,
                Gems = save.Wallet != null ? save.Wallet.Gems : 0,
            };
        }

        /// <summary>
        /// Which copy wins when a player signs in:
        /// <list type="number">
        /// <item>No cloud copy → upload local.</item>
        /// <item>Same timestamp → in sync.</item>
        /// <item>Fresh local install + cloud with progress → take cloud (reinstall / new phone).</item>
        /// <item>Otherwise the newer copy wins — unless the older one has strictly more progress, which
        /// is never silently discarded (e.g. a second device played offline for a week).</item>
        /// </list>
        /// </summary>
        public static CloudSaveDecision Decide(long localTicks, SaveProgressSummary local, CloudSaveSnapshot cloud)
        {
            if (cloud == null || string.IsNullOrEmpty(cloud.Json)) return CloudSaveDecision.UseLocal;
            if (cloud.LastSavedUtcTicks == localTicks) return CloudSaveDecision.InSync;
            if (local.IsFresh && !cloud.Progress.IsFresh) return CloudSaveDecision.UseCloud;
            if (!local.IsFresh && cloud.Progress.IsFresh) return CloudSaveDecision.UseLocal;

            bool cloudNewer = cloud.LastSavedUtcTicks > localTicks;
            if (cloudNewer)
                return local.Score > cloud.Progress.Score ? CloudSaveDecision.UseLocal : CloudSaveDecision.UseCloud;
            return cloud.Progress.Score > local.Score ? CloudSaveDecision.UseCloud : CloudSaveDecision.UseLocal;
        }

        /// <summary>Uploads are batched: at most one per interval, except when the app is backgrounded.</summary>
        public static bool ShouldUpload(bool pending, float now, float lastUploadTime, float minInterval, bool force) =>
            pending && (force || now - lastUploadTime >= minInterval);

        // ---------------------------------------------------------------- analytics

        public const int MaxEventNameLength = 40;
        public const int MaxParamNameLength = 40;
        public const int MaxParams = 25;
        public const int MaxStringValueLength = 100;

        /// <summary>Firebase event/param names: letters, digits, underscores; must start with a letter; ≤ 40 chars.</summary>
        public static string SanitizeName(string raw, int maxLength = MaxEventNameLength)
        {
            if (string.IsNullOrEmpty(raw)) return "unnamed";
            var sb = new StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                char lower = char.ToLowerInvariant(c);
                sb.Append((lower >= 'a' && lower <= 'z') || (lower >= '0' && lower <= '9') ? lower : '_');
            }
            string name = sb.ToString();
            if (name.Length == 0 || !(name[0] >= 'a' && name[0] <= 'z')) name = "e_" + name;
            // Reserved prefixes can't be used by apps.
            if (name.StartsWith("firebase_") || name.StartsWith("google_") || name.StartsWith("ga_")) name = "x_" + name;
            return name.Length > maxLength ? name.Substring(0, maxLength) : name;
        }

        /// <summary>Drops extra params, sanitizes names and truncates long strings.</summary>
        public static List<AnalyticsParam> SanitizeParams(IReadOnlyList<AnalyticsParam> parameters)
        {
            var result = new List<AnalyticsParam>();
            if (parameters == null) return result;
            foreach (var p in parameters)
            {
                if (result.Count >= MaxParams) break;
                object value = p.Value;
                if (value is string s && s.Length > MaxStringValueLength) value = s.Substring(0, MaxStringValueLength);
                else if (value is int i) value = (long)i;
                else if (value is float f) value = (double)f;
                else if (value is bool b) value = b ? 1L : 0L;
                else if (!(value is long || value is double || value is string)) value = value?.ToString() ?? string.Empty;
                result.Add(new AnalyticsParam(SanitizeName(p.Name, MaxParamNameLength), value));
            }
            return result;
        }

        // ---------------------------------------------------------------- leaderboard

        /// <summary>A run beats the stored best on wave reached; ties broken by kills.</summary>
        public static bool IsNewBest(int bestWave, int bestKills, int wave, int kills) =>
            wave > bestWave || (wave == bestWave && kills > bestKills);

        // ---------------------------------------------------------------- remote config

        /// <summary>Default Remote Config values (also in firebase/remote_config_defaults.json). Read via RemoteValues.</summary>
        public static Dictionary<string, object> RemoteConfigDefaults() => new()
        {
            { "xp_multiplier", 1.0 },
            { "coin_multiplier", 1.0 },
            { "battle_pass_season_id", "" },
            { "interstitial_every_n_runs", 3 },
            { "min_supported_version", "1.0" },
            { "cloud_save_enabled", true },
            { "leaderboard_enabled", true },
            { "event_cherry_blossom_enabled", true },
            { "event_oni_moon_enabled", true },
        };
    }
}
