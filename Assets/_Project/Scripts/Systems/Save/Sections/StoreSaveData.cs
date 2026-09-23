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
    }

    [Serializable]
    public class SkinSelection
    {
        public string HeroId;
        public string SkinId;
    }
}
