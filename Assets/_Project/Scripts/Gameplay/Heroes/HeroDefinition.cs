using System.Collections.Generic;
using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Gameplay.Pets;
using NinjaVillage.Gameplay.Skills;
using NinjaVillage.Gameplay.Ultimates;
using NinjaVillage.Gameplay.Weapons;
using NinjaVillage.Systems.Economy;
using UnityEngine;

namespace NinjaVillage.Gameplay.Heroes
{
    /// <summary>
    /// One playable ninja class (EPIC 13) — "every ninja changes gameplay": a stat profile with
    /// per-level growth, a signature weapon, optional starting skills and ultimate, optional
    /// companions (Beast Ninja's wolves), plus unlock/upgrade pricing. The Hero system applies it
    /// to the run through <c>HeroRunModifier</c>; nothing in battle reads it directly.
    /// </summary>
    [CreateAssetMenu(fileName = "Hero_New", menuName = "Ninja Village/Hero Definition")]
    public class HeroDefinition : DescriptiveScriptableObject
    {
        [Header("Class")]
        [Tooltip("Short line for UI, e.g. \"Fast attacks · Low HP\".")]
        [SerializeField] private string roleSummary;
        [Tooltip("Placeholder portrait tint until real art exists.")]
        [SerializeField] private Color themeColor = Color.white;
        [SerializeField] private int sortOrder;

        [Header("Unlock")]
        [Tooltip("Granted for free on first launch (the starter hero).")]
        [SerializeField] private bool unlockedByDefault;
        [SerializeField] private CurrencyType unlockCurrency = CurrencyType.Coins;
        [SerializeField, Min(0)] private int unlockCost = 1000;
        [Tooltip("Dojo level needed before this hero can be unlocked.")]
        [SerializeField, Min(0)] private int requiredDojoLevel;
        [Tooltip("Premium heroes (EPIC 21) are bought with gems or granted by a store purchase.")]
        [SerializeField] private bool isPremium;

        [Header("Progression")]
        [SerializeField, Min(1)] private int maxLevel = 30;
        [Tooltip("Coins to go from level 1 to 2; level L → L+1 costs base × L^exponent.")]
        [SerializeField, Min(0)] private int upgradeCostBase = 120;
        [SerializeField] private float upgradeCostExponent = 1.6f;

        [Header("Stats")]
        [SerializeField] private HeroStatBlock baseStats;
        [SerializeField] private HeroStatBlock statsPerLevel;

        [Header("Loadout (all optional)")]
        [Tooltip("Replaces the scene's default weapon. The Forge/inventory may still override or level it.")]
        [SerializeField] private WeaponDefinition signatureWeapon;
        [SerializeField] private List<SkillDefinition> startingSkills = new();
        [SerializeField] private UltimateDefinition ultimate;

        [Header("Companions (Beast Ninja)")]
        [SerializeField] private PetDefinition companionPet;
        [SerializeField, Min(0)] private int companionCount;
        [Tooltip("One extra companion every N hero levels (0 = never).")]
        [SerializeField, Min(0)] private int extraCompanionEveryLevels;
        [SerializeField, Min(0)] private int maxCompanions = 3;
        [Tooltip("Companion power relative to the same pet owned at that level.")]
        [SerializeField] private float companionPowerScale = 0.6f;

        public string RoleSummary => roleSummary;
        public Color ThemeColor => themeColor;
        public int SortOrder => sortOrder;
        public bool UnlockedByDefault => unlockedByDefault;
        public CurrencyType UnlockCurrency => unlockCurrency;
        public int UnlockCost => unlockCost;
        public int RequiredDojoLevel => requiredDojoLevel;
        public bool IsPremium => isPremium;
        public int MaxLevel => Mathf.Max(1, maxLevel);
        public int UpgradeCostBase => upgradeCostBase;
        public float UpgradeCostExponent => upgradeCostExponent;
        public HeroStatBlock BaseStats => baseStats;
        public HeroStatBlock StatsPerLevel => statsPerLevel;
        public WeaponDefinition SignatureWeapon => signatureWeapon;
        public IReadOnlyList<SkillDefinition> StartingSkills => startingSkills;
        public UltimateDefinition Ultimate => ultimate;
        public PetDefinition CompanionPet => companionPet;
        public int CompanionCount => companionCount;
        public int ExtraCompanionEveryLevels => extraCompanionEveryLevels;
        public int MaxCompanions => maxCompanions;
        public float CompanionPowerScale => companionPowerScale;

        /// <summary>Display name with the id as a fallback.</summary>
        public string NameOrId => string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;

        public HeroStatBlock GetStatsAtLevel(int level) => HeroStatBlock.AtLevel(baseStats, statsPerLevel, level);
    }
}
