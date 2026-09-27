using NinjaVillage.Core;
using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Gameplay.Loot;
using NinjaVillage.Systems.Economy;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    public enum MarketRewardType
    {
        Coins,
        Gems,
        /// <summary>A specific equipment piece (<see cref="MarketOfferDefinition.Equipment"/>) × amount.</summary>
        Equipment,
        /// <summary>Random equipment of <see cref="MarketOfferDefinition.RandomRarity"/> × amount — Forge materials.</summary>
        RandomEquipment
    }

    /// <summary>
    /// One entry in the Market's offer pool (EPIC 11 "Daily shop"). Each day the Market rolls a few of
    /// these (weighted, gated by Market level) into its stock; each can be bought once per day. Pure
    /// data so designers add deals without code: equipment, gems for coins, coin packs for gems, and
    /// crates of random equipment used as Forge crafting materials.
    /// </summary>
    [CreateAssetMenu(fileName = "MarketOffer_New", menuName = "Ninja Village/Village/Market Offer")]
    public class MarketOfferDefinition : DescriptiveScriptableObject
    {
        [Header("Reward")]
        [SerializeField] private MarketRewardType rewardType = MarketRewardType.Coins;
        [SerializeField, Min(1)] private int rewardAmount = 1;
        [SerializeField] private EquipmentDefinition equipment;
        [SerializeField] private Rarity randomRarity = Rarity.Common;

        [Header("Price")]
        [SerializeField] private CurrencyType priceCurrency = CurrencyType.Coins;
        [SerializeField, Min(0)] private int priceAmount = 100;

        [Header("Daily rotation")]
        [Tooltip("Relative chance to appear in a day's stock.")]
        [SerializeField, Min(0f)] private float weight = 1f;
        [SerializeField, Min(0)] private int requiredMarketLevel = 1;

        public MarketRewardType RewardType => rewardType;
        public int RewardAmount => rewardAmount;
        public EquipmentDefinition Equipment => equipment;
        public Rarity RandomRarity => randomRarity;
        public Price Price => new(priceCurrency, priceAmount);
        public float Weight => weight;
        public int RequiredMarketLevel => requiredMarketLevel;

        public string NameOrId => string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;

        /// <summary>"250 Coins", "Shadow Cloak", "2× random Rare gear".</summary>
        public string DescribeReward()
        {
            switch (rewardType)
            {
                case MarketRewardType.Coins: return $"{rewardAmount} Coins";
                case MarketRewardType.Gems: return $"{rewardAmount} Gems";
                case MarketRewardType.Equipment:
                {
                    string name = equipment != null
                        ? (string.IsNullOrEmpty(equipment.DisplayName) ? equipment.Id : equipment.DisplayName)
                        : "Equipment";
                    return rewardAmount > 1 ? $"{rewardAmount}× {name}" : name;
                }
                case MarketRewardType.RandomEquipment:
                    return $"{rewardAmount}× random {randomRarity} gear";
                default: return rewardType.ToString();
            }
        }
    }
}
