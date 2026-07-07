using NinjaVillage.Core;
using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Loot
{
    /// <summary>
    /// Data definition for a piece of dropped equipment. On pickup it applies a
    /// one-time stat bonus to PlayerStats — same push-model as skills.
    /// </summary>
    [CreateAssetMenu(fileName = "Equipment_New", menuName = "Ninja Village/Equipment")]
    public class EquipmentDefinition : DescriptiveScriptableObject
    {
        [Header("Rarity")]
        [SerializeField] private Rarity rarity = Rarity.Common;

        [Header("Stat Bonuses (flat additions to PlayerStats)")]
        [SerializeField] private float attackDamageBonus = 0f;
        [SerializeField] private float attackSpeedBonus = 0f;
        [SerializeField] private float moveSpeedBonus = 0f;
        [SerializeField] private float critChanceBonus = 0f;
        [SerializeField] private float critMultiplierBonus = 0f;
        [SerializeField] private float healPerSecondBonus = 0f;

        public Rarity Rarity => rarity;

        /// <summary>Applies this equipment's stat bonuses to the player once on pickup.</summary>
        public void Apply(PlayerStats stats)
        {
            if (attackDamageBonus != 0f) stats.AddAttackDamageMultiplier(attackDamageBonus);
            if (attackSpeedBonus != 0f) stats.AddAttackSpeedMultiplier(attackSpeedBonus);
            if (moveSpeedBonus != 0f) stats.AddMoveSpeedMultiplier(moveSpeedBonus);
            if (critChanceBonus != 0f) stats.AddCritChanceBonus(critChanceBonus);
            if (critMultiplierBonus != 0f) stats.AddCritMultiplierBonus(critMultiplierBonus);
            if (healPerSecondBonus != 0f) stats.AddHealPerSecond(healPerSecondBonus);
        }
    }
}
