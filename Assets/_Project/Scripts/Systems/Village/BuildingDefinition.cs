using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    /// <summary>
    /// Data for one village building (Dojo, Forge, Shrine, Pet House, Market, Castle — ids must be the
    /// <see cref="BuildingIds"/> constants, which is what the save and other systems key on).
    /// Holds its progression (max level, Castle gating, cost curve), its data-defined effect value
    /// per level (Dojo attack %, Forge weapon-level cap, Shrine blessing-rank cap, Market offers per
    /// day, Castle equipment slots) and how the placeholder village map draws it, so balancing and
    /// layout never need code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "Building_New", menuName = "Ninja Village/Village/Building")]
    public class BuildingDefinition : DescriptiveScriptableObject
    {
        [Header("Progression")]
        [SerializeField, Min(1)] private int maxLevel = 10;
        [Tooltip("Level on a fresh save. The Castle starts at 1 (the starting hut); everything else must be built.")]
        [SerializeField, Min(0)] private int startLevel;
        [Tooltip("Castle level needed before this can be built. Ignored for the Castle itself.")]
        [SerializeField, Min(0)] private int requiredCastleLevel = 1;
        [Tooltip("Max level allowed per Castle level (2 → Castle Lv3 allows Lv6). 0 = not gated.")]
        [SerializeField, Min(0)] private int levelsPerCastleLevel = 1;
        [Tooltip("Best wave needed per upgrade: level L → L+1 requires wave (L - startLevel + 1) × this. 0 = none.")]
        [SerializeField, Min(0)] private int waveRequirementPerLevel;
        [Tooltip("Price of each upgrade. Step 0 = the first purchase after startLevel.")]
        [SerializeField] private CostCurve upgradeCost = new();

        [Header("Effect (linear from level 1 to max level)")]
        [SerializeField] private EffectDisplay effectDisplay = EffectDisplay.None;
        [SerializeField] private string effectLabel;
        [SerializeField] private float effectAtFirstLevel;
        [SerializeField] private float effectAtMaxLevel;

        [Header("Village map (placeholder visuals)")]
        [SerializeField] private Vector2 plotPosition;
        [SerializeField] private Vector2 footprint = new(2.4f, 2f);
        [SerializeField] private Color color = new(0.72f, 0.56f, 0.40f, 1f);
        [SerializeField] private Color roofColor = new(0.62f, 0.20f, 0.18f, 1f);
        [Tooltip("Names as the building grows, e.g. Hut → House → Keep → Castle. Spread evenly over the levels.")]
        [SerializeField] private string[] stageNames = System.Array.Empty<string>();

        public int MaxLevel => maxLevel;
        public int StartLevel => startLevel;
        public int RequiredCastleLevel => requiredCastleLevel;
        public int LevelsPerCastleLevel => levelsPerCastleLevel;
        public int WaveRequirementPerLevel => waveRequirementPerLevel;
        public CostCurve UpgradeCost => upgradeCost;
        public EffectDisplay EffectDisplay => effectDisplay;
        public string EffectLabel => effectLabel;
        public Vector2 PlotPosition => plotPosition;
        public Vector2 Footprint => footprint;
        public Color Color => color;
        public Color RoofColor => roofColor;
        public bool IsCastle => Id == BuildingIds.Castle;

        public string NameOrId => string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;

        public float EffectAt(int level) => VillageRules.EffectAt(level, maxLevel, effectAtFirstLevel, effectAtMaxLevel);

        public string FormatEffect(float value) => VillageRules.FormatEffect(effectDisplay, effectLabel, value);

        public string FormatEffectAt(int level) => FormatEffect(EffectAt(level));

        /// <summary>Price to go from <paramref name="currentLevel"/> to the next level.</summary>
        public Price UpgradePrice(int currentLevel)
        {
            var curve = upgradeCost ?? new CostCurve();
            return curve.PriceAt(Mathf.Max(0, currentLevel - startLevel));
        }

        public int RequiredWaveFor(int currentLevel) => VillageRules.RequiredWave(currentLevel, startLevel, waveRequirementPerLevel);

        public int LevelCap(int castleLevel) => VillageRules.LevelCap(maxLevel, levelsPerCastleLevel, castleLevel, IsCastle);

        /// <summary>Stage name for the map label ("Temple"), falling back to the display name.</summary>
        public string StageName(int level)
        {
            int index = VillageRules.StageIndex(level, maxLevel, stageNames != null ? stageNames.Length : 0);
            if (index < 0 || string.IsNullOrEmpty(stageNames[index])) return NameOrId;
            return stageNames[index];
        }
    }
}
