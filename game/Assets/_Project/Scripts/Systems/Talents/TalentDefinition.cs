using System.Collections.Generic;
using NinjaVillage.Core.ScriptableObjects;
using UnityEngine;

namespace NinjaVillage.Systems.Talents
{
    /// <summary>The three branches of the talent tree (EPIC 14 "Talent categories").</summary>
    public enum TalentCategory
    {
        Offense,
        Defense,
        Utility
    }

    /// <summary>Which player stat a talent rank improves.</summary>
    public enum TalentStat
    {
        AttackDamage,
        AttackSpeed,
        CritChance,
        CritDamage,
        MaxHealth,
        DamageReduction,
        HealPerSecond,
        DodgeChance,
        StartingShield,
        MoveSpeed,
        XpGain,
        GoldGain,
        PickupRadius,
        LuckyDrop,
        UltimateCharge
    }

    /// <summary>
    /// One node of the permanent talent tree. Each rank adds <see cref="ValuePerRank"/> of
    /// <see cref="Stat"/> to every run and costs coins on a growing curve. A node can be ranked up
    /// once every prerequisite node has at least one rank — that's what makes it a tree rather than a
    /// flat stat shop. <see cref="Tier"/> is the row in the tree UI (0 = root).
    /// </summary>
    [CreateAssetMenu(fileName = "Talent_New", menuName = "Ninja Village/Talents/Talent")]
    public class TalentDefinition : DescriptiveScriptableObject
    {
        [Header("Tree placement")]
        [SerializeField] private TalentCategory category = TalentCategory.Offense;
        [SerializeField, Min(0)] private int tier;
        [Tooltip("Every one of these must have rank ≥ 1 before this node can be ranked up.")]
        [SerializeField] private List<TalentDefinition> prerequisites = new();

        [Header("Effect")]
        [SerializeField] private TalentStat stat = TalentStat.AttackDamage;
        [Tooltip("Per rank. Percent stats are fractions (0.03 = +3%); HealPerSecond/StartingShield are flat.")]
        [SerializeField] private float valuePerRank = 0.03f;
        [SerializeField, Min(1)] private int maxRank = 5;

        [Header("Cost (coins)")]
        [SerializeField, Min(0)] private int baseCost = 150;
        [Tooltip("Each rank costs this much more than the previous (1.5 = +50%).")]
        [SerializeField, Min(1f)] private float costGrowth = 1.5f;

        public TalentCategory Category => category;
        public int Tier => tier;
        public IReadOnlyList<TalentDefinition> Prerequisites => prerequisites;
        public TalentStat Stat => stat;
        public float ValuePerRank => valuePerRank;
        public int MaxRank => Mathf.Max(1, maxRank);
        public int BaseCost => baseCost;
        public float CostGrowth => costGrowth;

        public string NameOrId => string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;

        /// <summary>Coins to go from <paramref name="currentRank"/> to the next rank.</summary>
        public int CostForNextRank(int currentRank) => TalentRules.RankCost(baseCost, costGrowth, currentRank);

        public float ValueAt(int rank) => Mathf.Max(0, rank) * valuePerRank;
    }
}
