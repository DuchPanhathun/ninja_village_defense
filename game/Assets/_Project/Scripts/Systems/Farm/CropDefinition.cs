using NinjaVillage.Core.Data;
using NinjaVillage.Core.ScriptableObjects;
using UnityEngine;

namespace NinjaVillage.Systems.Farm
{
    /// <summary>
    /// A crop for the farm plots: what planting costs, how long it grows (real time), what the harvest
    /// gives, which castle level unlocks it, and how it looks at each stage on the map.
    /// </summary>
    [CreateAssetMenu(fileName = "Crop", menuName = "Ninja Village/Village/Crop")]
    public class CropDefinition : DescriptiveScriptableObject
    {
        [SerializeField] private GoodsDefinition harvest;
        [SerializeField, Min(1)] private int harvestAmount = 3;
        [SerializeField, Min(0)] private int seedCost = 10;
        [Tooltip("Real-time seconds from planting to ripe (before watering).")]
        [SerializeField, Min(1f)] private float growSeconds = 300f;
        [SerializeField, Min(1)] private int requiredCastleLevel = 1;

        [Header("Look on the map")]
        [SerializeField] private Sprite seedSprite;
        [SerializeField] private Sprite growingSprite;
        [SerializeField] private Sprite ripeSprite;

        public GoodsDefinition Harvest => harvest;
        public int HarvestAmount => harvestAmount;
        public int SeedCost => seedCost;
        public float GrowSeconds => growSeconds;
        public int RequiredCastleLevel => requiredCastleLevel;
        public Sprite SeedSprite => seedSprite;
        public Sprite GrowingSprite => growingSprite;
        public Sprite RipeSprite => ripeSprite;
        public string NameOrId => string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;
    }
}
