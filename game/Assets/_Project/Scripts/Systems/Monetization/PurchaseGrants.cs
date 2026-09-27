using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Heroes;
using NinjaVillage.Systems.LiveOps;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Monetization
{
    public enum GrantResult
    {
        Granted,
        /// <summary>This transaction was already granted (IAP re-delivery) — confirm without granting again.</summary>
        Duplicate,
        /// <summary>A restored non-consumable the player already has — nothing to do.</summary>
        AlreadyOwned,
        UnknownProduct
    }

    /// <summary>
    /// What each real-money product gives (EPIC 21), applied exactly once per store transaction:
    /// gem packs, the one-time starter pack (gems + coins + Beast Ninja), Remove Ads, and the premium
    /// battle pass for the current season. Called on IAP "pending" BEFORE the purchase is confirmed; the
    /// caller must save and only then confirm, so a crash can never lose a paid reward.
    /// </summary>
    public static class PurchaseGrants
    {
        public const int MaxLedger = 200;
        /// <summary>Gems refunded if the premium pass is bought while no season (or premium already) is active.</summary>
        public const int PassFallbackGems = 500;

        public static GrantResult Grant(string productId, string transactionId)
        {
            var store = SaveService.Data.Store;
            if (!string.IsNullOrEmpty(transactionId) && store.ProcessedTransactionIds.Contains(transactionId))
                return GrantResult.Duplicate;

            var result = Apply(productId, store);
            if (result == GrantResult.UnknownProduct) return result;

            if (!string.IsNullOrEmpty(transactionId))
            {
                store.ProcessedTransactionIds.Add(transactionId);
                if (store.ProcessedTransactionIds.Count > MaxLedger)
                    store.ProcessedTransactionIds.RemoveRange(0, store.ProcessedTransactionIds.Count - MaxLedger);
            }
            if (result == GrantResult.Granted)
                Progress.Report(ProgressStatIds.Purchase, 1, productId);
            return result;
        }

        private static GrantResult Apply(string productId, StoreSaveData store)
        {
            var product = StoreProducts.Get(productId);
            if (product == null) return GrantResult.UnknownProduct;

            int gems = StoreProducts.GemsFor(productId);
            if (gems > 0)
            {
                CurrencyService.Grant(CurrencyType.Gems, gems, productId);
                return GrantResult.Granted;
            }

            switch (productId)
            {
                case StoreProducts.StarterPack:
                    if (store.StarterPackPurchased) return GrantResult.AlreadyOwned;
                    store.StarterPackPurchased = true;
                    MarkOwned(store, productId);
                    CurrencyService.Grant(CurrencyType.Gems, StoreProducts.StarterPackGems, productId);
                    CurrencyService.Grant(CurrencyType.Coins, StoreProducts.StarterPackCoins, productId);
                    HeroService.Grant(StoreProducts.StarterPackHeroId);
                    return GrantResult.Granted;

                case StoreProducts.RemoveAds:
                    if (store.AdsRemoved) return GrantResult.AlreadyOwned;
                    store.AdsRemoved = true;
                    MarkOwned(store, productId);
                    return GrantResult.Granted;

                case StoreProducts.BattlePassPremium:
                    var season = BattlePassService.EnsureSeason();
                    if (season != null && BattlePassService.UnlockPremium())
                    {
                        if (!store.PremiumPassSeasons.Contains(season.Id)) store.PremiumPassSeasons.Add(season.Id);
                    }
                    else
                    {
                        // Bought between seasons or twice: never keep money without giving value.
                        CurrencyService.Grant(CurrencyType.Gems, PassFallbackGems, productId);
                    }
                    return GrantResult.Granted;
            }
            return GrantResult.UnknownProduct;
        }

        private static void MarkOwned(StoreSaveData store, string productId)
        {
            if (!store.PurchasedProductIds.Contains(productId)) store.PurchasedProductIds.Add(productId);
        }

        public static bool Owns(string productId) => SaveService.Data.Store.PurchasedProductIds.Contains(productId);
    }
}
