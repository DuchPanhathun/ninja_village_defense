using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    /// <summary>Why a blessing rank can't be bought (None = it can).</summary>
    public enum BlessingBlocker
    {
        None,
        NoDefinition,
        ShrineNotBuilt,
        /// <summary>This blessing needs a higher Shrine level to unlock at all.</summary>
        Locked,
        MaxRank,
        /// <summary>The Shrine's current level caps the rank.</summary>
        ShrineLevel,
        NotEnoughCurrency
    }

    /// <summary>
    /// Shrine "Passive blessing" logic (EPIC 11): buying blessing ranks, the Shrine-level rank cap,
    /// and reporting <see cref="ProgressStatIds.BlessingUnlocked"/>. The resulting bonuses are applied
    /// to runs by <see cref="VillageRunModifier"/> via <see cref="VillageService.ComputeBonuses"/>.
    /// </summary>
    public static class ShrineService
    {
        public static BlessingCatalog Catalog => CatalogCache<BlessingCatalog>.Get();

        public static int ShrineLevel => VillageService.GetLevel(BuildingIds.Shrine);

        public static int GetRank(string blessingId) => VillageService.Data.GetBlessingRank(blessingId);

        /// <summary>Highest rank the current Shrine allows for <paramref name="def"/> (fallback: one rank per Shrine level).</summary>
        public static int RankCap(BlessingDefinition def)
        {
            if (def == null) return 0;
            float shrineEffect = VillageService.GetEffect(BuildingIds.Shrine, ShrineLevel);
            return VillageRules.BlessingRankCap(def.MaxRank, shrineEffect);
        }

        public static BlessingBlocker Check(BlessingDefinition def)
        {
            if (def == null) return BlessingBlocker.NoDefinition;
            int rank = GetRank(def.Id);
            var price = def.PriceForNextRank(rank);
            return Check(rank, def.MaxRank, RankCap(def), ShrineLevel, def.RequiredShrineLevel, price, CurrencyService.Balance(price.Currency));
        }

        /// <summary>Pure rule, testable with primitives.</summary>
        public static BlessingBlocker Check(int rank, int maxRank, int rankCap, int shrineLevel, int requiredShrineLevel, Price price, int balance)
        {
            if (shrineLevel <= 0) return BlessingBlocker.ShrineNotBuilt;
            if (shrineLevel < requiredShrineLevel) return BlessingBlocker.Locked;
            if (rank >= maxRank) return BlessingBlocker.MaxRank;
            if (rank >= rankCap) return BlessingBlocker.ShrineLevel;
            if (!price.IsFree && balance < price.Amount) return BlessingBlocker.NotEnoughCurrency;
            return BlessingBlocker.None;
        }

        public static bool TryBless(BlessingDefinition def, out BlessingBlocker blocker)
        {
            blocker = Check(def);
            if (blocker != BlessingBlocker.None) return false;

            int rank = GetRank(def.Id);
            if (!CurrencyService.TrySpend(def.PriceForNextRank(rank), def.Id))
            {
                blocker = BlessingBlocker.NotEnoughCurrency;
                return false;
            }

            int newRank = rank + 1;
            VillageService.Data.Blessings.SetLevel(def.Id, newRank);
            SaveService.SaveNow();

            Progress.Report(ProgressStatIds.BlessingUnlocked, 1, def.Id);
            EventBus<BlessingRankChangedEvent>.Raise(new BlessingRankChangedEvent(def.Id, newRank));
            return true;
        }

        public static string DescribeBlocker(BlessingBlocker blocker, BlessingDefinition def)
        {
            switch (blocker)
            {
                case BlessingBlocker.None: return string.Empty;
                case BlessingBlocker.ShrineNotBuilt: return "Build the Shrine first";
                case BlessingBlocker.Locked: return def != null ? $"Requires Shrine Lv {def.RequiredShrineLevel}" : "Locked";
                case BlessingBlocker.MaxRank: return "Max rank reached";
                case BlessingBlocker.ShrineLevel: return $"Upgrade the Shrine to Lv {Mathf.Max(ShrineLevel + 1, 1)} for more ranks";
                case BlessingBlocker.NotEnoughCurrency:
                    return def != null && def.PriceForNextRank(GetRank(def.Id)).Currency == CurrencyType.Gems ? "Not enough gems" : "Not enough coins";
                default: return "Unavailable";
            }
        }
    }
}
