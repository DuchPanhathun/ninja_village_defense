using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Inventory;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Village
{
    /// <summary>Why an offer can't be bought (None = it can).</summary>
    public enum MarketBlocker
    {
        None,
        Closed,
        NotInStock,
        SoldOut,
        NoDefinition,
        /// <summary>The reward can't be granted (e.g. equipment offer with no equipment assigned).</summary>
        NoReward,
        NotEnoughCurrency
    }

    /// <summary>
    /// Market "Daily shop" (EPIC 11): keeps today's stock in the save (rolled by
    /// <see cref="MarketRules"/> from <see cref="GameClock.Today"/>), sells each offer once per day,
    /// grants the reward (currency or equipment into the persistent inventory) and reports
    /// <see cref="ProgressStatIds.MarketPurchase"/>.
    /// </summary>
    public static class MarketService
    {
        /// <summary>Offers per day if the Market definition is missing.</summary>
        public const int FallbackOfferCount = 3;

        private static readonly List<MarketCandidate> Candidates = new();

        public static MarketOfferCatalog Catalog => CatalogCache<MarketOfferCatalog>.Get();

        public static int MarketLevel => VillageService.GetLevel(BuildingIds.Market);

        public static bool IsOpen => MarketLevel > 0;

        public static int OfferCount =>
            System.Math.Max(1, VillageRules.FloorEffect(VillageService.GetEffect(BuildingIds.Market, FallbackOfferCount)));

        /// <summary>Today's stock (after <see cref="EnsureStock"/>).</summary>
        public static IReadOnlyList<MarketOfferState> Stock => VillageService.Data.MarketOffers;

        public static MarketOfferDefinition GetOffer(string offerId)
        {
            var catalog = Catalog;
            return catalog != null ? catalog.Get(offerId) : null;
        }

        /// <summary>Re-rolls on a new day / tops up after a Market upgrade. Safe to call every time the shop is shown.</summary>
        public static bool EnsureStock()
        {
            if (!IsOpen) return false;
            var catalog = Catalog;
            if (catalog == null) return false;

            Candidates.Clear();
            foreach (var def in catalog.All)
            {
                if (def == null) continue;
                Candidates.Add(new MarketCandidate(def.Id, def.Weight, def.RequiredMarketLevel));
            }

            int today = GameClock.Today;
            int seed = MarketRules.SeedFor(today, SaveService.Data.Profile.PlayerId);
            bool changed = MarketRules.RefreshStock(VillageService.Data, Candidates, today, MarketLevel, OfferCount, seed);
            if (changed)
            {
                SaveService.MarkDirty();
                EventBus<MarketStockChangedEvent>.Raise(new MarketStockChangedEvent(null));
            }
            return changed;
        }

        public static MarketBlocker Check(string offerId)
        {
            if (!IsOpen) return MarketBlocker.Closed;
            var state = VillageService.Data.FindMarketOffer(offerId);
            if (state == null) return MarketBlocker.NotInStock;
            if (state.Purchased) return MarketBlocker.SoldOut;
            var def = GetOffer(offerId);
            if (def == null) return MarketBlocker.NoDefinition;
            if (!CanGrant(def)) return MarketBlocker.NoReward;
            if (!CurrencyService.CanAfford(def.Price)) return MarketBlocker.NotEnoughCurrency;
            return MarketBlocker.None;
        }

        /// <summary>Buys one offer. <paramref name="rewardSummary"/> describes what was received ("2× Iron Ring").</summary>
        public static bool TryBuy(string offerId, out MarketBlocker blocker, out string rewardSummary)
        {
            rewardSummary = string.Empty;
            EnsureStock(); // the day may have rolled over while the screen was open

            blocker = Check(offerId);
            if (blocker != MarketBlocker.None) return false;

            var def = GetOffer(offerId);
            if (!CurrencyService.TrySpend(def.Price, offerId))
            {
                blocker = MarketBlocker.NotEnoughCurrency;
                return false;
            }

            rewardSummary = GrantReward(def);
            var state = VillageService.Data.FindMarketOffer(offerId);
            if (state != null) state.Purchased = true;
            SaveService.SaveNow();

            Progress.Report(ProgressStatIds.MarketPurchase, 1, offerId);
            EventBus<MarketStockChangedEvent>.Raise(new MarketStockChangedEvent(offerId));
            return true;
        }

        public static string DescribeBlocker(MarketBlocker blocker, MarketOfferDefinition def)
        {
            switch (blocker)
            {
                case MarketBlocker.None: return string.Empty;
                case MarketBlocker.Closed: return "Build the Market first";
                case MarketBlocker.NotInStock: return "Not in today's stock";
                case MarketBlocker.SoldOut: return "Sold out — back tomorrow";
                case MarketBlocker.NoReward: return "This offer is unavailable";
                case MarketBlocker.NotEnoughCurrency:
                    return def != null && def.Price.Currency == CurrencyType.Gems ? "Not enough gems" : "Not enough coins";
                default: return "Unavailable";
            }
        }

        private static bool CanGrant(MarketOfferDefinition def)
        {
            switch (def.RewardType)
            {
                case MarketRewardType.Equipment: return def.Equipment != null && !string.IsNullOrEmpty(def.Equipment.Id);
                case MarketRewardType.RandomEquipment: return InventoryService.HasEquipmentOfRarity(def.RandomRarity);
                default: return def.RewardAmount > 0;
            }
        }

        private static string GrantReward(MarketOfferDefinition def)
        {
            switch (def.RewardType)
            {
                case MarketRewardType.Coins:
                    CurrencyService.Grant(CurrencyType.Coins, def.RewardAmount, def.Id);
                    return $"+{def.RewardAmount} Coins";

                case MarketRewardType.Gems:
                    CurrencyService.Grant(CurrencyType.Gems, def.RewardAmount, def.Id);
                    return $"+{def.RewardAmount} Gems";

                case MarketRewardType.Equipment:
                    InventoryService.AddEquipment(def.Equipment.Id, def.RewardAmount);
                    return $"Got {def.DescribeReward()}";

                case MarketRewardType.RandomEquipment:
                {
                    var names = new List<string>();
                    for (int i = 0; i < def.RewardAmount; i++)
                    {
                        var piece = InventoryService.RandomEquipmentOfRarity(def.RandomRarity);
                        if (piece == null) continue;
                        InventoryService.AddEquipment(piece.Id);
                        names.Add(DefinitionNames.Of(piece));
                    }
                    return names.Count > 0 ? $"Got {string.Join(", ", names)}" : string.Empty;
                }

                default:
                    return string.Empty;
            }
        }
    }
}
