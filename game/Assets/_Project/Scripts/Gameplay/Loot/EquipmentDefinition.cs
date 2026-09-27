using NinjaVillage.Core;
using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Loot
{
    /// <summary>What kind of gear a piece is — shown as a small type icon on its tiles.</summary>
    public enum GearKind
    {
        Ring,
        Amulet,
        Armor,
        Helmet,
        Talisman,
        Boots,
    }

    /// <summary>
    /// Data definition for a piece of dropped equipment. On pickup it applies a
    /// one-time stat bonus to PlayerStats — same push-model as skills. The listed bonuses are for its native
    /// grade (from <see cref="Rarity"/>); owned copies of other grades scale them (pass the scale).
    /// </summary>
    [CreateAssetMenu(fileName = "Equipment_New", menuName = "Ninja Village/Equipment")]
    public class EquipmentDefinition : DescriptiveScriptableObject
    {
        [Header("Rarity")]
        [SerializeField] private Rarity rarity = Rarity.Common;
        [SerializeField] private GearKind kind = GearKind.Ring;
        [Tooltip("S-class: a special piece only found in Surprise Boxes (drops at Elite, stronger than regular gear).")]
        [SerializeField] private bool sClass;

        [Header("Stat Bonuses (flat additions to PlayerStats)")]
        [SerializeField] private float attackDamageBonus = 0f;
        [SerializeField] private float attackSpeedBonus = 0f;
        [SerializeField] private float moveSpeedBonus = 0f;
        [SerializeField] private float critChanceBonus = 0f;
        [SerializeField] private float critMultiplierBonus = 0f;
        [SerializeField] private float healPerSecondBonus = 0f;

        public Rarity Rarity => rarity;
        public GearKind Kind => kind;
        public bool IsSpecial => sClass;
        public float AttackDamageBonus => attackDamageBonus;

        /// <summary>"+10% attack damage · +5% crit chance" — the bonuses in words (times <paramref name="scale"/>), for item cards.</summary>
        public string DescribeBonuses(string separator = " · ", float scale = 1f)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (attackDamageBonus != 0f) parts.Add($"{attackDamageBonus * scale * 100f:+0;-0}% attack damage");
            if (attackSpeedBonus != 0f) parts.Add($"{attackSpeedBonus * scale * 100f:+0;-0}% attack speed");
            if (moveSpeedBonus != 0f) parts.Add($"{moveSpeedBonus * scale * 100f:+0;-0}% move speed");
            if (critChanceBonus != 0f) parts.Add($"{critChanceBonus * scale * 100f:+0.#;-0.#}% crit chance");
            if (critMultiplierBonus != 0f) parts.Add($"{critMultiplierBonus * scale * 100f:+0;-0}% crit damage");
            if (healPerSecondBonus != 0f) parts.Add($"{healPerSecondBonus * scale:+0.##;-0.##} HP/s");
            return string.Join(separator, parts);
        }

        /// <summary>Applies this equipment's stat bonuses (times <paramref name="scale"/>, for its grade) to the player.</summary>
        public void Apply(PlayerStats stats, float scale = 1f)
        {
            if (attackDamageBonus != 0f) stats.AddAttackDamageMultiplier(attackDamageBonus * scale);
            if (attackSpeedBonus != 0f) stats.AddAttackSpeedMultiplier(attackSpeedBonus * scale);
            if (moveSpeedBonus != 0f) stats.AddMoveSpeedMultiplier(moveSpeedBonus * scale);
            if (critChanceBonus != 0f) stats.AddCritChanceBonus(critChanceBonus * scale);
            if (critMultiplierBonus != 0f) stats.AddCritMultiplierBonus(critMultiplierBonus * scale);
            if (healPerSecondBonus != 0f) stats.AddHealPerSecond(healPerSecondBonus * scale);
        }
    }
}
