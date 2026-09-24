using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Village;
using UnityEngine;

namespace NinjaVillage.Systems.Requests
{
    public enum RequestKind
    {
        /// <summary>Hand over goods from the storehouse ("Bring 5 Carrots").</summary>
        Deliver,
        /// <summary>Do something a progress stat counts, from the day it's posted ("Defeat 150 enemies").</summary>
        Stat,
        /// <summary>Clear the next chapter you haven't cleared yet.</summary>
        Chapter,
    }

    /// <summary>
    /// A kind of favour a villager can ask for (EPIC 24 Phase 3). <see cref="DescriptiveScriptableObject.DisplayName"/>
    /// is the task as a format string — {0} the amount (or chapter number), {1} the goods' name — and
    /// <see cref="Line"/> what the villager says. Requests only come up once the village can do them
    /// (castle / kitchen level), and pay coins (more with a bigger castle), sometimes gems or a decoration.
    /// </summary>
    [CreateAssetMenu(fileName = "Request", menuName = "Ninja Village/Village/Villager Request")]
    public class VillagerRequestDefinition : DescriptiveScriptableObject
    {
        [SerializeField] private RequestKind kind;
        [SerializeField] private GoodsDefinition goods;
        [Tooltip("ProgressStatIds value for Stat requests.")]
        [SerializeField] private string statId;
        [SerializeField, Min(1)] private int amount = 1;
        [TextArea] [SerializeField] private string line;
        [Tooltip("Villager sprite keys that like to ask this (empty = anyone).")]
        [SerializeField] private string[] villagers = System.Array.Empty<string>();

        [Header("When it can come up")]
        [SerializeField, Min(1)] private int requiredCastleLevel = 1;
        [SerializeField, Min(0)] private int requiredKitchenLevel;
        [SerializeField, Min(0f)] private float weight = 1f;

        [Header("Reward")]
        [SerializeField, Min(0)] private int rewardCoins = 100;
        [SerializeField, Min(0)] private int rewardGems;
        [SerializeField] private DecorationDefinition rewardDecoration;

        public RequestKind Kind => kind;
        public GoodsDefinition Goods => goods;
        public string StatId => statId;
        public int Amount => amount;
        public string Line => line;
        public string[] Villagers => villagers;
        public int RequiredCastleLevel => requiredCastleLevel;
        public int RequiredKitchenLevel => requiredKitchenLevel;
        public float Weight => weight;
        public int RewardCoins => rewardCoins;
        public int RewardGems => rewardGems;
        public DecorationDefinition RewardDecoration => rewardDecoration;
    }
}
