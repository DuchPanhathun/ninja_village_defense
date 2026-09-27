using NinjaVillage.Core.Data;
using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Systems.Talents;
using UnityEngine;

namespace NinjaVillage.Systems.Farm
{
    public enum GoodsCategory
    {
        Crop,
        Meal,
        Fish,
        Ore,
        Other,
    }

    /// <summary>
    /// Something kept in the village storehouse: a harvested crop now, meals, fish and ore in later phases.
    /// <see cref="DescriptiveScriptableObject.Icon"/> is its picture; it can be sold for <see cref="SellPrice"/> coins.
    /// Meals also carry the boost they give when eaten before a battle (<see cref="MealStat"/> by <see cref="MealValue"/>).
    /// </summary>
    [CreateAssetMenu(fileName = "Goods", menuName = "Ninja Village/Village/Goods")]
    public class GoodsDefinition : DescriptiveScriptableObject
    {
        [SerializeField] private GoodsCategory category;
        [SerializeField, Min(0)] private int sellPrice = 5;
        [SerializeField] private int sortOrder;

        [Header("Meal (eaten at the start of a battle)")]
        [SerializeField] private TalentStat mealStat;
        [Tooltip("0 = not a battle meal. Same units as talents: 0.1 = +10%.")]
        [SerializeField] private float mealValue;

        public GoodsCategory Category => category;
        public int SellPrice => sellPrice;
        public int SortOrder => sortOrder;
        public TalentStat MealStat => mealStat;
        public float MealValue => mealValue;
        public bool IsBattleMeal => mealValue > 0f;
        /// <summary>"+10% max health", or empty for goods that aren't battle meals.</summary>
        public string MealEffect => IsBattleMeal ? TalentService.FormatValue(mealStat, mealValue) : string.Empty;
        public string NameOrId => string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;
    }
}
