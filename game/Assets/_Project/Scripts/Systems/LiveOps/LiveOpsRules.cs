using System;
using System.Collections.Generic;
using System.Globalization;

namespace NinjaVillage.Systems.LiveOps
{
    /// <summary>Live-ops rules with no Unity dependencies (EditMode-tested).</summary>
    public static class LiveOpsRules
    {
        /// <summary>"2026-09-01" → UTC midnight; invalid → null.</summary>
        public static DateTime? ParseDate(string yyyyMMdd)
        {
            if (string.IsNullOrEmpty(yyyyMMdd)) return null;
            return DateTime.TryParseExact(yyyyMMdd, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var date)
                ? date
                : (DateTime?)null;
        }

        /// <summary>Start inclusive, end exclusive.</summary>
        public static bool IsWithin(DateTime nowUtc, DateTime? startUtc, int durationDays) =>
            startUtc.HasValue && nowUtc >= startUtc.Value && nowUtc < startUtc.Value.AddDays(Math.Max(0, durationDays));

        // ---------------------------------------------------------------- battle pass

        /// <summary>Tiers unlocked by <paramref name="xp"/> (0..tierCount).</summary>
        public static int TiersReached(int xp, int xpPerTier, int tierCount) =>
            xpPerTier <= 0 ? 0 : Math.Min(Math.Max(0, tierCount), Math.Max(0, xp) / xpPerTier);

        /// <summary>XP progress inside the current tier as 0..1 (1 when the pass is complete).</summary>
        public static float TierProgress(int xp, int xpPerTier, int tierCount)
        {
            if (xpPerTier <= 0) return 0f;
            if (TiersReached(xp, xpPerTier, tierCount) >= tierCount) return 1f;
            return (Math.Max(0, xp) % xpPerTier) / (float)xpPerTier;
        }

        public enum ClaimResult { Ok, NotReached, AlreadyClaimed, PremiumLocked, InvalidTier }

        /// <param name="tierIndex">0-based tier.</param>
        public static ClaimResult CheckClaim(int tierIndex, int tiersReached, int tierCount, bool premiumTrack, bool premiumUnlocked, ICollection<int> claimed)
        {
            if (tierIndex < 0 || tierIndex >= tierCount) return ClaimResult.InvalidTier;
            if (tierIndex >= tiersReached) return ClaimResult.NotReached;
            if (premiumTrack && !premiumUnlocked) return ClaimResult.PremiumLocked;
            if (claimed != null && claimed.Contains(tierIndex)) return ClaimResult.AlreadyClaimed;
            return ClaimResult.Ok;
        }

        /// <summary>
        /// Pass XP for a finished run: a flat amount for playing plus a bonus per wave (capped), so short
        /// runs still progress the pass but pushing further is worth more.
        /// </summary>
        public static int RunXp(int waveReached, bool victory) =>
            30 + Math.Min(Math.Max(0, waveReached) * 5, 150) + (victory ? 50 : 0);

        // ---------------------------------------------------------------- limited offers

        public static bool CanPurchase(int purchasedSoFar, int purchaseLimit) =>
            purchaseLimit <= 0 || purchasedSoFar < purchaseLimit;
    }
}
