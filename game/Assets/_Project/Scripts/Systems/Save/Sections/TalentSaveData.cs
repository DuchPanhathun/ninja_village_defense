using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>
    /// Unlocked talent nodes (EPIC 14). Owned by the Talent system. The invested totals record
    /// what the player actually paid, so a reset refunds exactly that even if prices are
    /// rebalanced later.
    /// </summary>
    [Serializable]
    public class TalentSaveData
    {
        /// <summary>Talent node id → rank.</summary>
        public List<IdLevelEntry> Nodes = new();
        public int TimesReset;
        /// <summary>Coins spent on talents since the last reset — refunded in full by a reset.</summary>
        public int CoinsSpent;

        /// <summary>Coins spent on ranks since the last reset (refunded by a reset).</summary>
        public int CoinsInvested;
        /// <summary>Gems spent on ranks since the last reset (refunded by a reset).</summary>
        public int GemsInvested;

        public int GetRank(string talentId) => Nodes.GetLevel(talentId);

        /// <summary>Sum of all ranks bought (talent "points spent").</summary>
        public int TotalRanks
        {
            get
            {
                int total = 0;
                if (Nodes == null) return 0;
                foreach (var entry in Nodes)
                    if (entry != null && entry.Level > 0) total += entry.Level;
                return total;
            }
        }

        /// <summary>Repairs lists a hand-edited / cloud-merged save may have nulled.</summary>
        public void EnsureInitialized()
        {
            Nodes ??= new List<IdLevelEntry>();
        }
    }
}
