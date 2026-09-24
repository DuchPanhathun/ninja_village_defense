using System;
using System.Collections.Generic;
using NinjaVillage.Core.Config;
using NinjaVillage.Core.Data;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.LiveOps
{
    /// <summary>Raised when pass XP, claims, premium state or the active season change.</summary>
    public readonly struct BattlePassChangedEvent : IGameEvent { }

    /// <summary>
    /// Battle pass (EPIC 20). The current season is picked by date (or forced with the Remote Config key
    /// <c>battle_pass_season_id</c>). When the season changes, progress resets (XP, claims, premium).
    /// Monetization calls <see cref="UnlockPremium"/> after the "battle_pass_premium" purchase.
    ///
    /// XP sources (fed by <see cref="LiveOpsTracker"/>): finished runs (<see cref="LiveOpsRules.RunXp"/>),
    /// bosses, completed quests/missions, daily logins and evolution discoveries.
    /// </summary>
    public static class BattlePassService
    {
        public static SeasonCatalog Catalog => CatalogLoader.Load<SeasonCatalog>();

        private static BattlePassSaveData Data
        {
            get
            {
                var liveOps = SaveService.Data.LiveOps;
                liveOps.BattlePass ??= new BattlePassSaveData();
                var bp = liveOps.BattlePass;
                bp.ClaimedFreeTiers ??= new List<int>();
                bp.ClaimedPremiumTiers ??= new List<int>();
                return bp;
            }
        }

        public static SeasonDefinition CurrentSeason
        {
            get
            {
                var catalog = Catalog;
                if (catalog == null) return null;

                string forced = RemoteValues.GetString("battle_pass_season_id", string.Empty);
                if (!string.IsNullOrEmpty(forced))
                {
                    var season = catalog.Get(forced);
                    if (season != null) return season;
                }

                DateTime now = GameClock.UtcNow;
                foreach (var season in catalog.All)
                    if (season != null && season.IsActive(now)) return season;
                return null;
            }
        }

        public static SeasonDefinition NextSeason
        {
            get
            {
                var catalog = Catalog;
                if (catalog == null) return null;
                DateTime now = GameClock.UtcNow;
                SeasonDefinition best = null;
                foreach (var season in catalog.All)
                {
                    if (season == null || !season.StartUtc.HasValue || season.StartUtc.Value <= now) continue;
                    if (best == null || season.StartUtc.Value < best.StartUtc.Value) best = season;
                }
                return best;
            }
        }

        /// <summary>Resets progress when a new season starts. Returns the active season (or null).</summary>
        public static SeasonDefinition EnsureSeason()
        {
            var season = CurrentSeason;
            var data = Data;
            if (season != null && data.SeasonId != season.Id)
            {
                data.SeasonId = season.Id;
                data.Xp = 0;
                data.PremiumUnlocked = false;
                data.ClaimedFreeTiers.Clear();
                data.ClaimedPremiumTiers.Clear();
                SaveService.MarkDirty();
                EventBus<BattlePassChangedEvent>.Raise(new BattlePassChangedEvent());
            }
            return season;
        }

        public static int Xp => Data.Xp;
        public static bool IsPremiumUnlocked => Data.PremiumUnlocked;

        public static int TiersReached
        {
            get
            {
                var season = EnsureSeason();
                return season == null ? 0 : LiveOpsRules.TiersReached(Data.Xp, season.XpPerTier, season.TierCount);
            }
        }

        public static void AddXp(int amount, string subject = null)
        {
            if (amount <= 0 || EnsureSeason() == null) return;
            Data.Xp += amount;
            SaveService.MarkDirty();
            EventBus<BattlePassChangedEvent>.Raise(new BattlePassChangedEvent());
        }

        /// <summary>Called by the store after the premium pass purchase is confirmed. Idempotent.</summary>
        public static bool UnlockPremium()
        {
            if (EnsureSeason() == null || Data.PremiumUnlocked) return false;
            Data.PremiumUnlocked = true;
            SaveService.SaveNow();
            EventBus<BattlePassChangedEvent>.Raise(new BattlePassChangedEvent());
            return true;
        }

        public static LiveOpsRules.ClaimResult CheckClaim(int tier, bool premium)
        {
            var season = EnsureSeason();
            if (season == null) return LiveOpsRules.ClaimResult.InvalidTier;
            var claimed = premium ? Data.ClaimedPremiumTiers : Data.ClaimedFreeTiers;
            return LiveOpsRules.CheckClaim(tier, TiersReached, season.TierCount, premium, Data.PremiumUnlocked, claimed);
        }

        public static bool IsClaimed(int tier, bool premium) =>
            (premium ? Data.ClaimedPremiumTiers : Data.ClaimedFreeTiers).Contains(tier);

        public static bool TryClaim(int tier, bool premium)
        {
            if (CheckClaim(tier, premium) != LiveOpsRules.ClaimResult.Ok) return false;
            var season = CurrentSeason;
            var reward = premium ? season.PremiumReward(tier) : season.FreeReward(tier);
            (premium ? Data.ClaimedPremiumTiers : Data.ClaimedFreeTiers).Add(tier);
            LiveOpsRewards.GrantAll(new[] { reward }, $"bp_{season.Id}_{tier}");
            EventBus<BattlePassChangedEvent>.Raise(new BattlePassChangedEvent());
            return true;
        }

        public static int ClaimableCount
        {
            get
            {
                var season = EnsureSeason();
                if (season == null) return 0;
                int count = 0;
                for (int tier = 0; tier < TiersReached; tier++)
                {
                    if (season.HasFreeReward(tier) && !IsClaimed(tier, false)) count++;
                    if (Data.PremiumUnlocked && !IsClaimed(tier, true)) count++;
                }
                return count;
            }
        }

        public static TimeSpan TimeLeft
        {
            get
            {
                var season = CurrentSeason;
                return season != null && season.EndUtc.HasValue ? season.EndUtc.Value - GameClock.UtcNow : TimeSpan.Zero;
            }
        }
    }
}
