using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Systems.Economy;
using UnityEngine;

namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// Data for one companion (EPIC 12): which ability it uses, how it levels, what it costs and
    /// what passive bonus it gives its owner. Pets work without art — with no <see cref="Prefab"/>
    /// <see cref="PetFactory"/> builds a colored placeholder sprite in code — so new pets are pure
    /// data. Also used for the Beast Ninja's wolf companions.
    /// </summary>
    [CreateAssetMenu(fileName = "Pet_New", menuName = "Ninja Village/Pet Definition")]
    public class PetDefinition : DescriptiveScriptableObject
    {
        [Header("Behaviour")]
        [SerializeField] private PetAbilityType abilityType = PetAbilityType.None;
        [Tooltip("Short line for UI, e.g. \"Collects XP\".")]
        [SerializeField] private string abilitySummary;
        [SerializeField] private int sortOrder;

        [Header("Visuals (all optional)")]
        [Tooltip("Optional. Without a prefab the pet is built in code as a colored placeholder sprite.")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private Color placeholderColor = Color.white;
        [SerializeField] private float placeholderScale = 0.6f;
        [Tooltip("Optional ability prefab: Hawk = coin to reveal (gets a CoinPickup), Monkey = banana visual, Dragon = breath visual.")]
        [SerializeField] private GameObject abilityPrefab;
        [Tooltip("Idle orbit distance from the player.")]
        [SerializeField] private float followDistance = 1.4f;

        [Header("Unlock")]
        [SerializeField] private CurrencyType unlockCurrency = CurrencyType.Coins;
        [SerializeField, Min(0)] private int unlockCost = 500;
        [Tooltip("Pet House level needed before this pet can be unlocked.")]
        [SerializeField, Min(0)] private int requiredPetHouseLevel;
        [Tooltip("Premium pets (EPIC 21) are bought with gems or granted by a store purchase.")]
        [SerializeField] private bool isPremium;

        [Header("Progression")]
        [SerializeField, Min(1)] private int maxLevel = 20;
        [Tooltip("Coins to go from level 1 to 2; later levels scale by level^exponent.")]
        [SerializeField, Min(0)] private int upgradeCostBase = 80;
        [SerializeField] private float upgradeCostExponent = 1.55f;

        [Header("Stats")]
        [SerializeField] private PetStatTemplate stats = PetStatTemplate.Default;
        [Tooltip("Share of the owner's bonus damage the pet inherits (0 = none, 1 = all).")]
        [SerializeField, Range(0f, 1f)] private float ownerDamageInheritance = 0.5f;
        [SerializeField] private PetOwnerBonus ownerBonus;
        [SerializeField] private PetOwnerBonus ownerBonusPerLevel;

        public PetAbilityType AbilityType => abilityType;
        public string AbilitySummary => abilitySummary;
        public int SortOrder => sortOrder;
        public GameObject Prefab => prefab;
        public Color PlaceholderColor => placeholderColor;
        public float PlaceholderScale => placeholderScale;
        public GameObject AbilityPrefab => abilityPrefab;
        public float FollowDistance => followDistance;
        public CurrencyType UnlockCurrency => unlockCurrency;
        public int UnlockCost => unlockCost;
        public int RequiredPetHouseLevel => requiredPetHouseLevel;
        public bool IsPremium => isPremium;
        public int MaxLevel => Mathf.Max(1, maxLevel);
        public int UpgradeCostBase => upgradeCostBase;
        public float UpgradeCostExponent => upgradeCostExponent;
        public PetStatTemplate Stats => stats;
        public float OwnerDamageInheritance => ownerDamageInheritance;
        public PetOwnerBonus OwnerBonus => ownerBonus;
        public PetOwnerBonus OwnerBonusPerLevel => ownerBonusPerLevel;

        /// <summary>Display name with the id as a fallback (placeholder assets may leave it empty).</summary>
        public string NameOrId => string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;

        public PetRuntimeStats GetStats(int level, in PetEquipmentBonus gear) => PetStatMath.Build(stats, level, gear);

        public PetOwnerBonus GetOwnerBonus(int level) => PetOwnerBonus.AtLevel(ownerBonus, ownerBonusPerLevel, level);
    }
}
