using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>Season progress on the battle pass (EPIC 20).</summary>
    [Serializable]
    public class BattlePassSaveData
    {
        public string SeasonId;
        public int Xp;
        public bool PremiumUnlocked;
        public List<int> ClaimedFreeTiers = new();
        public List<int> ClaimedPremiumTiers = new();
    }

    /// <summary>Live-ops state: battle pass, event missions, limited-time shop purchases (EPIC 20).</summary>
    [Serializable]
    public class LiveOpsSaveData
    {
        public BattlePassSaveData BattlePass = new();
        public string ActiveEventId;
        public List<QuestProgress> EventMissions = new();
        /// <summary>Limited-time offer id → times purchased.</summary>
        public List<IdLevelEntry> LimitedOfferPurchases = new();
    }
}
