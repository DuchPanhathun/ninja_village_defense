using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>One villager's request for today: who asks, what (resolved amount / chapter), progress, and whether it's done.</summary>
    [Serializable]
    public class VillagerRequestState
    {
        public string Id;
        public string VillagerKey;
        public int Target;
        /// <summary>Counted progress for stat requests (deliveries read the storehouse, chapters the chapter save).</summary>
        public int Progress;
        /// <summary>Chapter number for "Clear Chapter N" requests.</summary>
        public int Chapter;
        public bool Delivered;
    }

    /// <summary>Villager requests (EPIC 24 Phase 3): the three posted on <see cref="Day"/> (a GameClock day index).</summary>
    [Serializable]
    public class RequestSaveData
    {
        public int Day = -1;
        public List<VillagerRequestState> Active = new();
    }
}
