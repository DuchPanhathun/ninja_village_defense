using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>
    /// Stable building ids shared by every system that reads village progress
    /// (e.g. the Hero system checks the Dojo level, the Pet system the Pet House level).
    /// BuildingDefinition assets must use exactly these ids.
    /// </summary>
    public static class BuildingIds
    {
        public const string Dojo = "dojo";
        public const string Forge = "forge";
        public const string Shrine = "shrine";
        public const string PetHouse = "pet_house";
        public const string Market = "market";
        public const string Castle = "castle";
        public const string Kitchen = "kitchen";
    }

    /// <summary>One purchasable slot in the Market's daily rotation.</summary>
    [Serializable]
    public class MarketOfferState
    {
        public string OfferId;
        public bool Purchased;
    }

    /// <summary>A decoration the player bought and placed on their village map.</summary>
    [Serializable]
    public class PlacedDecoration
    {
        /// <summary>Unique per village (two identical lanterns are two uids).</summary>
        public int Uid;
        public string Id;
        public float X;
        public float Y;
        public bool Flip;
    }

    /// <summary>Village buildings and the Market's rotating stock (EPIC 11). Owned by the Village system.</summary>
    [Serializable]
    public class VillageSaveData
    {
        /// <summary>Building id → level. 0 / missing = not built yet.</summary>
        public List<IdLevelEntry> Buildings = new();

        /// <summary>Shrine blessing id → rank.</summary>
        public List<IdLevelEntry> Blessings = new();

        /// <summary>GameClock day index the current Market stock was rolled for.</summary>
        public int MarketStockDay = -1;
        public List<MarketOfferState> MarketOffers = new();

        /// <summary>Bought decorations and where they stand.</summary>
        public List<PlacedDecoration> Decorations = new();
        public int NextDecorationUid = 1;

        public int GetBuildingLevel(string buildingId) => Buildings.GetLevel(buildingId);

        public int GetBlessingRank(string blessingId) => Blessings.GetLevel(blessingId);

        /// <summary>Sum of every building's level — drives how busy/decorated the village map looks.</summary>
        public int TotalBuildingLevels()
        {
            int total = 0;
            if (Buildings == null) return 0;
            foreach (var entry in Buildings)
                if (entry != null && entry.Level > 0) total += entry.Level;
            return total;
        }

        public MarketOfferState FindMarketOffer(string offerId)
        {
            if (MarketOffers == null || string.IsNullOrEmpty(offerId)) return null;
            foreach (var offer in MarketOffers)
                if (offer != null && offer.OfferId == offerId) return offer;
            return null;
        }
    }
}
