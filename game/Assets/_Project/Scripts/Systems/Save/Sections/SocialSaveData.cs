using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>What this player did in someone else's village (so each button works once a day / week).</summary>
    [Serializable]
    public class VisitMark
    {
        public string VillageId;
        public int VisitDay = -1;
        public int LikedWeek = -1;
        public int GiftDay = -1;
        public int WaterDay = -1;
        public long WaterTicks;
    }

    /// <summary>Social (EPIC 24 Phase 6): visits paid and received, likes, gifts, help, and the weekly trophy.</summary>
    [Serializable]
    public class SocialSaveData
    {
        /// <summary>Villages this player visited, with what they left there.</summary>
        public List<VisitMark> MyVisits = new();

        /// <summary>Likes this village got in <see cref="LikesWeek"/> (distinct visitors).</summary>
        public int Likes;
        public int LikesWeek = -1;
        /// <summary>Visitor id → the last gift day already turned into coins.</summary>
        public List<IdLevelEntry> GiftsClaimed = new();
        /// <summary>Visitor id → the last watering day already applied to the farm.</summary>
        public List<IdLevelEntry> WateringsApplied = new();
        /// <summary>The week whose Best Village result was last checked (trophies are given once).</summary>
        public int TrophyCheckedWeek = -1;
        public int TrophiesWon;

        public VisitMark Mark(string villageId)
        {
            foreach (var mark in MyVisits)
                if (mark != null && mark.VillageId == villageId) return mark;
            var created = new VisitMark { VillageId = villageId };
            MyVisits.Add(created);
            return created;
        }
    }
}
