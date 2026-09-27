using NinjaVillage.Core.Events;

namespace NinjaVillage.Systems.Village
{
    /// <summary>Raised after a building is built or upgraded — the village map regrows it and screens refresh.</summary>
    public readonly struct BuildingUpgradedEvent : IGameEvent
    {
        public readonly string BuildingId;
        public readonly int NewLevel;
        public BuildingUpgradedEvent(string buildingId, int newLevel)
        {
            BuildingId = buildingId;
            NewLevel = newLevel;
        }
    }

    /// <summary>Raised after a Shrine blessing gains a rank.</summary>
    public readonly struct BlessingRankChangedEvent : IGameEvent
    {
        public readonly string BlessingId;
        public readonly int NewRank;
        public BlessingRankChangedEvent(string blessingId, int newRank)
        {
            BlessingId = blessingId;
            NewRank = newRank;
        }
    }

    /// <summary>Raised when the Market's stock is re-rolled (new day) or an offer is bought.</summary>
    public readonly struct MarketStockChangedEvent : IGameEvent
    {
        public readonly string PurchasedOfferId;
        public MarketStockChangedEvent(string purchasedOfferId) => PurchasedOfferId = purchasedOfferId;
    }
}
