using System;
using System.Collections.Generic;
using NinjaVillage.Core.ScriptableObjects;
using UnityEngine;

namespace NinjaVillage.Systems.LiveOps
{
    /// <summary>
    /// A battle pass season (EPIC 20 "Battle Pass"): a UTC date window, a tier track and one free and one
    /// premium reward per tier. Premium is unlocked by the "battle_pass_premium" purchase and applies
    /// retroactively to tiers already reached.
    /// </summary>
    [CreateAssetMenu(fileName = "Season_New", menuName = "Ninja Village/Live Ops/Battle Pass Season")]
    public class SeasonDefinition : DescriptiveScriptableObject
    {
        [Tooltip("UTC start date, yyyy-MM-dd.")]
        [SerializeField] private string startDate = "2026-09-01";
        [SerializeField, Min(1)] private int durationDays = 60;
        [SerializeField, Min(1)] private int xpPerTier = 300;
        [SerializeField] private List<LiveOpsReward> freeRewards = new();
        [SerializeField] private List<LiveOpsReward> premiumRewards = new();

        public DateTime? StartUtc => LiveOpsRules.ParseDate(startDate);
        public DateTime? EndUtc => StartUtc?.AddDays(durationDays);
        public int DurationDays => durationDays;
        public int XpPerTier => xpPerTier;
        public int TierCount => Mathf.Max(freeRewards.Count, premiumRewards.Count);

        public LiveOpsReward FreeReward(int tier) => tier >= 0 && tier < freeRewards.Count ? freeRewards[tier] : default;
        public LiveOpsReward PremiumReward(int tier) => tier >= 0 && tier < premiumRewards.Count ? premiumRewards[tier] : default;
        public bool HasFreeReward(int tier) => tier >= 0 && tier < freeRewards.Count && freeRewards[tier].amount > 0;

        public bool IsActive(DateTime nowUtc) => LiveOpsRules.IsWithin(nowUtc, StartUtc, durationDays);

#if UNITY_EDITOR
        public void EditorSetTrack(string start, int days, int xp, IEnumerable<LiveOpsReward> free, IEnumerable<LiveOpsReward> premium)
        {
            startDate = start;
            durationDays = days;
            xpPerTier = xp;
            freeRewards = new List<LiveOpsReward>(free);
            premiumRewards = new List<LiveOpsReward>(premium);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
