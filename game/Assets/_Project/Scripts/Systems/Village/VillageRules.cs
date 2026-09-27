using NinjaVillage.Systems.Economy;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    /// <summary>How a building's effect value is shown ("+12% Attack", "Offers per day: 5").</summary>
    public enum EffectDisplay
    {
        None,
        Percent,
        Integer
    }

    /// <summary>Why a building can't be upgraded right now (None = it can).</summary>
    public enum UpgradeBlocker
    {
        None,
        NoDefinition,
        MaxLevel,
        /// <summary>The Castle isn't high enough to build this at all.</summary>
        Locked,
        /// <summary>Built, but the current Castle level caps it.</summary>
        CastleLevel,
        /// <summary>Castle upgrades also need a deeper best wave.</summary>
        WaveRequirement,
        NotEnoughCurrency
    }

    /// <summary>Everything the upgrade gate looks at, as primitives so the rule is testable without assets or a save.</summary>
    public struct UpgradeQuery
    {
        public int CurrentLevel;
        public int MaxLevel;
        public bool IsCastle;
        public int CastleLevel;
        public int RequiredCastleLevel;
        public int LevelsPerCastleLevel;
        public int HighestWave;
        public int RequiredWave;
        public Price Cost;
        public int Balance;
    }

    /// <summary>
    /// The village's progression rules as pure functions (EPIC 11 "Main progression"): the Castle's
    /// level caps every other building and unlocks new ones, Castle upgrades themselves need deeper
    /// waves, and building effects scale linearly from their level-1 to max-level value. Kept free of
    /// Unity objects and save access so EditMode tests pin the rules down.
    /// </summary>
    public static class VillageRules
    {
        /// <summary>Highest level the building may reach with the current Castle.</summary>
        public static int LevelCap(int maxLevel, int levelsPerCastleLevel, int castleLevel, bool isCastle)
        {
            if (isCastle || levelsPerCastleLevel <= 0) return maxLevel;
            return Mathf.Clamp(castleLevel * levelsPerCastleLevel, 0, maxLevel);
        }

        /// <summary>Castle level needed before <paramref name="targetLevel"/> is allowed.</summary>
        public static int CastleLevelNeededFor(int targetLevel, int levelsPerCastleLevel, int requiredCastleLevel)
        {
            if (levelsPerCastleLevel <= 0) return requiredCastleLevel;
            int byCap = Mathf.CeilToInt(targetLevel / (float)levelsPerCastleLevel);
            return Mathf.Max(requiredCastleLevel, byCap);
        }

        /// <summary>Best wave required to buy the next level (0 = no requirement).</summary>
        public static int RequiredWave(int currentLevel, int startLevel, int wavePerLevel)
        {
            if (wavePerLevel <= 0) return 0;
            return Mathf.Max(0, currentLevel - startLevel + 1) * wavePerLevel;
        }

        /// <summary>Linear from <paramref name="atFirstLevel"/> (level 1) to <paramref name="atMaxLevel"/> (max level); 0 when not built.</summary>
        public static float EffectAt(int level, int maxLevel, float atFirstLevel, float atMaxLevel)
        {
            if (level <= 0) return 0f;
            if (maxLevel <= 1) return atFirstLevel;
            float t = Mathf.Clamp01((level - 1) / (float)(maxLevel - 1));
            return Mathf.Lerp(atFirstLevel, atMaxLevel, t);
        }

        /// <summary>Integer effects (slots, offers, caps) — epsilon guards float error like 2.9999999.</summary>
        public static int FloorEffect(float value) => Mathf.FloorToInt(value + 0.0001f);

        /// <summary>Which visual/name stage a level falls into; -1 when there are no stages.</summary>
        public static int StageIndex(int level, int maxLevel, int stageCount)
        {
            if (stageCount <= 0) return -1;
            if (level <= 1 || maxLevel <= 1) return 0;
            int index = (level - 1) * stageCount / maxLevel;
            return Mathf.Clamp(index, 0, stageCount - 1);
        }

        public static UpgradeBlocker Check(in UpgradeQuery q)
        {
            if (q.CurrentLevel >= q.MaxLevel) return UpgradeBlocker.MaxLevel;
            if (!q.IsCastle && q.CastleLevel < q.RequiredCastleLevel) return UpgradeBlocker.Locked;
            if (q.CurrentLevel >= LevelCap(q.MaxLevel, q.LevelsPerCastleLevel, q.CastleLevel, q.IsCastle))
                return UpgradeBlocker.CastleLevel;
            if (q.RequiredWave > 0 && q.HighestWave < q.RequiredWave) return UpgradeBlocker.WaveRequirement;
            if (!q.Cost.IsFree && q.Balance < q.Cost.Amount) return UpgradeBlocker.NotEnoughCurrency;
            return UpgradeBlocker.None;
        }

        public static string FormatEffect(EffectDisplay display, string label, float value)
        {
            switch (display)
            {
                case EffectDisplay.Percent: return $"+{value * 100f:0.#}% {label}";
                case EffectDisplay.Integer: return $"{label}: {FloorEffect(value)}";
                default: return string.Empty;
            }
        }

        public static string FormatBlessing(BlessingStat stat, float value)
        {
            string pct = $"+{value * 100f:0.#}%";
            switch (stat)
            {
                case BlessingStat.MaxHealth: return $"{pct} Max Health";
                case BlessingStat.CritChance: return $"{pct} Critical Chance";
                case BlessingStat.Luck: return $"{pct} Lucky Drop";
                case BlessingStat.CoinGain: return $"{pct} Coins";
                case BlessingStat.XpGain: return $"{pct} XP";
                case BlessingStat.DamageReduction: return $"{pct} Damage Reduction";
                default: return pct;
            }
        }

        /// <summary>Rank cap from the Shrine: its effect value, never above the blessing's own max.</summary>
        public static int BlessingRankCap(int maxRank, float shrineEffect) =>
            Mathf.Clamp(FloorEffect(shrineEffect), 0, maxRank);
    }
}
