using NinjaVillage.Core.Data;
using NinjaVillage.Core.ScriptableObjects;
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
    /// </summary>
    [CreateAssetMenu(fileName = "Goods", menuName = "Ninja Village/Village/Goods")]
    public class GoodsDefinition : DescriptiveScriptableObject
    {
        [SerializeField] private GoodsCategory category;
        [SerializeField, Min(0)] private int sellPrice = 5;
        [SerializeField] private int sortOrder;

        public GoodsCategory Category => category;
        public int SellPrice => sellPrice;
        public int SortOrder => sortOrder;
        public string NameOrId => string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;
    }
}
