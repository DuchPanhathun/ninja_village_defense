using NinjaVillage.Core;
using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Systems.Economy;
using UnityEngine;

namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// A piece of pet gear (collar, bell, charm...) bought once in the Pet screen and worn by one
    /// pet at a time (EPIC 12 "Pet equipment"). Its <see cref="Bonus"/> is layered on top of the
    /// pet's level stats by <see cref="PetStatMath.Build"/> — separate from the player's own
    /// run-drop <c>EquipmentDefinition</c>, which buffs the player instead.
    /// </summary>
    [CreateAssetMenu(fileName = "PetGear_New", menuName = "Ninja Village/Pet Equipment")]
    public class PetEquipmentDefinition : DescriptiveScriptableObject
    {
        [SerializeField] private Rarity rarity = Rarity.Common;
        [SerializeField] private int sortOrder;

        [Header("Cost")]
        [SerializeField] private CurrencyType costCurrency = CurrencyType.Coins;
        [SerializeField, Min(0)] private int cost = 400;
        [Tooltip("Pet House level needed before this gear can be bought.")]
        [SerializeField, Min(0)] private int requiredPetHouseLevel;

        [Header("Bonus")]
        [SerializeField] private PetEquipmentBonus bonus;

        public Rarity Rarity => rarity;
        public int SortOrder => sortOrder;
        public CurrencyType CostCurrency => costCurrency;
        public int Cost => cost;
        public int RequiredPetHouseLevel => requiredPetHouseLevel;
        public PetEquipmentBonus Bonus => bonus;

        public string NameOrId => string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;
    }
}
