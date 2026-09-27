using System;

namespace NinjaVillage.Systems.Talents
{
    public enum TalentResult
    {
        Success,
        InvalidTalent,
        MaxRank,
        PrerequisitesMissing,
        NotEnoughCoins,
        NothingToReset,
        NotEnoughGems
    }

    /// <summary>
    /// Talent-tree math with no Unity dependencies (EditMode-tested):
    /// <list type="bullet">
    /// <item>Rank cost: <c>baseCost × growth^rank</c>, rounded to 5 coins.</item>
    /// <item>Reset: refunds 100% of coins spent (so experimenting is never punished) and costs gems —
    /// the first reset is free, then <c>ResetGemBase × (resets so far)</c>, capped — so the tree stays a
    /// meaningful choice rather than something to re-spec before every run.</item>
    /// </list>
    /// </summary>
    public static class TalentRules
    {
        public const int ResetGemBase = 20;
        public const int ResetGemCap = 200;

        public static int RankCost(int baseCost, float growth, int currentRank)
        {
            if (baseCost <= 0) return 0;
            double raw = baseCost * Math.Pow(Math.Max(1.0, growth), Math.Max(0, currentRank));
            long rounded = (long)Math.Round(raw / 5.0, MidpointRounding.AwayFromZero) * 5;
            return (int)Math.Min(int.MaxValue, Math.Max(5, rounded));
        }

        /// <summary>Total coins for ranks 0 → <paramref name="rank"/>.</summary>
        public static long TotalCostUpTo(int baseCost, float growth, int rank)
        {
            long total = 0;
            for (int r = 0; r < rank; r++) total += RankCost(baseCost, growth, r);
            return total;
        }

        public static TalentResult CheckRankUp(int currentRank, int maxRank, bool prerequisitesMet, int coins, int cost)
        {
            if (currentRank >= maxRank) return TalentResult.MaxRank;
            if (!prerequisitesMet) return TalentResult.PrerequisitesMissing;
            if (coins < cost) return TalentResult.NotEnoughCoins;
            return TalentResult.Success;
        }

        /// <summary>Gems a reset costs, given how many resets were already done.</summary>
        public static int ResetGemCost(int timesResetSoFar) =>
            timesResetSoFar <= 0 ? 0 : Math.Min(ResetGemCap, ResetGemBase * timesResetSoFar);

        public static TalentResult CheckReset(int totalRanks, int timesResetSoFar, int gems)
        {
            if (totalRanks <= 0) return TalentResult.NothingToReset;
            if (gems < ResetGemCost(timesResetSoFar)) return TalentResult.NotEnoughGems;
            return TalentResult.Success;
        }
    }
}
