using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>Purchases and entitlements (EPIC 21). Owned by the Monetization system.</summary>
    [Serializable]
    public class StoreSaveData
    {
        public bool AdsRemoved;
        public List<string> PurchasedProductIds = new();
        public List<string> OwnedSkinIds = new();
        /// <summary>Which skin each hero wears (hero id → skin id).</summary>
        public List<SkinSelection> EquippedSkins = new();
        public bool StarterPackPurchased;
        /// <summary>Store transaction ids already granted — IAP can re-deliver a pending order, never grant twice. Capped.</summary>
        public List<string> ProcessedTransactionIds = new();
        /// <summary>Battle pass seasons whose premium track was bought (the purchase is a consumable per season).</summary>
        public List<string> PremiumPassSeasons = new();
        /// <summary>Completed runs since the last interstitial ad (ad pacing).</summary>
        public int RunsSinceInterstitial;
        /// <summary>GameClock day of <see cref="AdGemsClaimed"/> (the "watch an ad for gems" daily cap).</summary>
        public int AdGemsDay = -1;
        public int AdGemsClaimed;
    }

    [Serializable]
    public class SkinSelection
    {
        public string HeroId;
        public string SkinId;
    }
}
