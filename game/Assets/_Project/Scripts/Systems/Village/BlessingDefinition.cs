using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Systems.Economy;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    /// <summary>The run stat a Shrine blessing raises. Mapped onto PlayerStats / RunStartContext by <see cref="VillageBonuses"/>.</summary>
    public enum BlessingStat
    {
        /// <summary>+% max health (RunStartContext.MaxHealthMultiplier).</summary>
        MaxHealth,
        /// <summary>+ critical chance (PlayerStats.AddCritChanceBonus).</summary>
        CritChance,
        /// <summary>+ lucky-drop chance: doubled coin drops and a better equipment drop chance.</summary>
        Luck,
        /// <summary>+% coins from every source that honors the Gold Bonus multiplier.</summary>
        CoinGain,
        XpGain,
        DamageReduction
    }

    /// <summary>
    /// A Shrine "passive blessing" (goal.text: +Health, +Critical, +Luck, +Coins). Players buy ranks at
    /// the Shrine; ranks are stored in <c>VillageSaveData.Blessings</c> and applied to every run by the
    /// VillageRunModifier. The Shrine's own level caps how many ranks can be bought, so upgrading the
    /// building and the blessings are two interleaved goals.
    /// </summary>
    [CreateAssetMenu(fileName = "Blessing_New", menuName = "Ninja Village/Village/Blessing")]
    public class BlessingDefinition : DescriptiveScriptableObject
    {
        [SerializeField] private BlessingStat stat = BlessingStat.MaxHealth;
        [Tooltip("Stat gained per rank (0.05 = +5%).")]
        [SerializeField] private float valuePerRank = 0.05f;
        [SerializeField, Min(1)] private int maxRank = 10;
        [SerializeField, Min(1)] private int requiredShrineLevel = 1;
        [Tooltip("Price of each rank. Step 0 = rank 1.")]
        [SerializeField] private CostCurve rankCost = new(CostCurveType.Exponential, CurrencyType.Coins, 120, 1.4f);
        [SerializeField] private Color color = new(1f, 0.85f, 0.5f, 1f);

        public BlessingStat Stat => stat;
        public float ValuePerRank => valuePerRank;
        public int MaxRank => maxRank;
        public int RequiredShrineLevel => requiredShrineLevel;
        public Color Color => color;

        public string NameOrId => string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;

        public float ValueAt(int rank) => Mathf.Max(0, rank) * valuePerRank;

        public Price PriceForNextRank(int currentRank)
        {
            var curve = rankCost ?? new CostCurve(CostCurveType.Exponential, CurrencyType.Coins, 120, 1.4f);
            return curve.PriceAt(Mathf.Max(0, currentRank));
        }

        public string FormatValue(float value) => VillageRules.FormatBlessing(stat, value);
    }
}
