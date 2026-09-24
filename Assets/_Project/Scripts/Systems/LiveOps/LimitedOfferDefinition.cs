using System;
using System.Collections.Generic;
using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Systems.Economy;
using UnityEngine;

namespace NinjaVillage.Systems.LiveOps
{
    /// <summary>
    /// A limited-time shop offer (EPIC 20 "Limited-time shop"): available while its seasonal event runs
    /// (or in its own date window), bought with coins or gems, with a per-player purchase limit.
    /// </summary>
    [CreateAssetMenu(fileName = "LimitedOffer_New", menuName = "Ninja Village/Live Ops/Limited Offer")]
    public class LimitedOfferDefinition : DescriptiveScriptableObject
    {
        [Tooltip("If set, the offer is available exactly while this event is active (dates below are ignored).")]
        [SerializeField] private string eventId;
        [Tooltip("UTC start date, yyyy-MM-dd (when not tied to an event).")]
        [SerializeField] private string startDate;
        [SerializeField, Min(1)] private int durationDays = 7;

        [SerializeField] private CurrencyType priceCurrency = CurrencyType.Gems;
        [SerializeField, Min(0)] private int priceAmount = 50;
        [Tooltip("0 = unlimited.")]
        [SerializeField, Min(0)] private int purchaseLimit = 1;
        [SerializeField] private string badge = "LIMITED";
        [SerializeField] private List<LiveOpsReward> rewards = new();

        public string EventId => eventId;
        public DateTime? StartUtc => LiveOpsRules.ParseDate(startDate);
        public DateTime? EndUtc => StartUtc?.AddDays(durationDays);
        public int DurationDays => durationDays;
        public Price Price => new(priceCurrency, priceAmount);
        public int PurchaseLimit => purchaseLimit;
        public string Badge => badge;
        public IReadOnlyList<LiveOpsReward> Rewards => rewards;

#if UNITY_EDITOR
        public void EditorSetRewards(IEnumerable<LiveOpsReward> newRewards)
        {
            rewards = new List<LiveOpsReward>(newRewards);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
