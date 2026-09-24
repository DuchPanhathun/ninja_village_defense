using System.Collections.Generic;
using NinjaVillage.Core.ScriptableObjects;
using UnityEngine;

namespace NinjaVillage.Systems.Daily
{
    /// <summary>
    /// A permanent, tiered achievement (EPIC 19 "Achievement system"): e.g. "Demon Slayer — defeat
    /// 100 / 1,000 / 10,000 demons", each tier paying its own reward. Progress on
    /// <see cref="StatId"/> accumulates forever (or tracks the best value for max stats like
    /// wave_reached). Tier targets must be ascending.
    /// </summary>
    [CreateAssetMenu(fileName = "Achievement_New", menuName = "Ninja Village/Daily/Achievement")]
    public class AchievementDefinition : DescriptiveScriptableObject
    {
        [SerializeField] private string statId = "enemies_killed";
        [SerializeField] private List<int> tierTargets = new() { 10, 100, 1000 };
        [SerializeField] private List<DailyReward> tierRewards = new();
        [SerializeField] private int sortOrder;

        public string StatId => statId;
        public IReadOnlyList<int> TierTargets => tierTargets;
        public int TierCount => tierTargets.Count;
        public int SortOrder => sortOrder;

        public DailyReward RewardForTier(int tierIndex) =>
            tierIndex >= 0 && tierIndex < tierRewards.Count ? tierRewards[tierIndex] : DailyReward.Gems(5 * (tierIndex + 1));

        public int TargetForTier(int tierIndex) =>
            tierTargets.Count == 0 ? 0 : tierTargets[Mathf.Clamp(tierIndex, 0, tierTargets.Count - 1)];
    }
}
