using System.Text;
using UnityEngine;

namespace NinjaVillage.Gameplay.Heroes
{
    /// <summary>
    /// A hero's stat modifiers (EPIC 13 "Hero stats") — used twice on <see cref="HeroDefinition"/>:
    /// once for level 1 and once as the per-level growth. Every field is additive onto the matching
    /// <c>PlayerStats</c> multiplier / <c>RunStartContext</c> value, so 0 means "no change" and a
    /// negative number is a drawback (the Assassin's low HP). A plain serializable struct with public
    /// fields so tests and generators can build it directly.
    /// </summary>
    [System.Serializable]
    public struct HeroStatBlock
    {
        [Tooltip("Additive max-health multiplier: 0.2 = +20%, -0.15 = -15%.")]
        public float maxHealthPercent;
        [Tooltip("Flat max health added before the multiplier.")]
        public float maxHealthFlat;
        [Tooltip("0.1 = +10% attack damage.")]
        public float attackDamagePercent;
        [Tooltip("0.1 = +10% attacks per second.")]
        public float attackSpeedPercent;
        [Tooltip("0.1 = +10% move speed.")]
        public float moveSpeedPercent;
        [Tooltip("0.05 = +5% crit chance.")]
        public float critChance;
        [Tooltip("0.25 = +0.25× crit multiplier.")]
        public float critDamage;
        [Tooltip("Fraction of hit damage ignored (PlayerStats caps the total at 80%).")]
        public float damageReduction;
        [Tooltip("HP regenerated per second.")]
        public float healPerSecond;
        [Tooltip("0.05 = +5% dodge chance.")]
        public float dodgeChance;
        [Tooltip("Shield points the run starts with.")]
        public float startingShield;
        [Tooltip("0.1 = +10% XP gained.")]
        public float xpGainPercent;
        [Tooltip("0.1 = +10% coins from drops.")]
        public float goldBonusPercent;
        [Tooltip("0.1 = +10% ultimate charge per kill.")]
        public float ultimateChargePercent;

        public static HeroStatBlock operator +(HeroStatBlock a, HeroStatBlock b) => new HeroStatBlock
        {
            maxHealthPercent = a.maxHealthPercent + b.maxHealthPercent,
            maxHealthFlat = a.maxHealthFlat + b.maxHealthFlat,
            attackDamagePercent = a.attackDamagePercent + b.attackDamagePercent,
            attackSpeedPercent = a.attackSpeedPercent + b.attackSpeedPercent,
            moveSpeedPercent = a.moveSpeedPercent + b.moveSpeedPercent,
            critChance = a.critChance + b.critChance,
            critDamage = a.critDamage + b.critDamage,
            damageReduction = a.damageReduction + b.damageReduction,
            healPerSecond = a.healPerSecond + b.healPerSecond,
            dodgeChance = a.dodgeChance + b.dodgeChance,
            startingShield = a.startingShield + b.startingShield,
            xpGainPercent = a.xpGainPercent + b.xpGainPercent,
            goldBonusPercent = a.goldBonusPercent + b.goldBonusPercent,
            ultimateChargePercent = a.ultimateChargePercent + b.ultimateChargePercent
        };

        public static HeroStatBlock operator *(HeroStatBlock a, float s) => new HeroStatBlock
        {
            maxHealthPercent = a.maxHealthPercent * s,
            maxHealthFlat = a.maxHealthFlat * s,
            attackDamagePercent = a.attackDamagePercent * s,
            attackSpeedPercent = a.attackSpeedPercent * s,
            moveSpeedPercent = a.moveSpeedPercent * s,
            critChance = a.critChance * s,
            critDamage = a.critDamage * s,
            damageReduction = a.damageReduction * s,
            healPerSecond = a.healPerSecond * s,
            dodgeChance = a.dodgeChance * s,
            startingShield = a.startingShield * s,
            xpGainPercent = a.xpGainPercent * s,
            goldBonusPercent = a.goldBonusPercent * s,
            ultimateChargePercent = a.ultimateChargePercent * s
        };

        /// <summary>Hero progression: level-1 stats plus (level − 1) × growth.</summary>
        public static HeroStatBlock AtLevel(in HeroStatBlock baseStats, in HeroStatBlock growthPerLevel, int level) =>
            baseStats + growthPerLevel * Mathf.Max(0, level - 1);

        /// <summary>Multi-line "+20% Max HP" summary of the non-zero stats, for the Hero screen.</summary>
        public string Describe(string separator = "\n")
        {
            var sb = new StringBuilder();
            Percent(sb, separator, maxHealthPercent, "Max HP");
            Flat(sb, separator, maxHealthFlat, "Max HP");
            Percent(sb, separator, attackDamagePercent, "Attack");
            Percent(sb, separator, attackSpeedPercent, "Attack Speed");
            Percent(sb, separator, moveSpeedPercent, "Move Speed");
            Percent(sb, separator, critChance, "Crit Chance");
            Flat(sb, separator, critDamage, "× Crit Damage");
            Percent(sb, separator, damageReduction, "Damage Reduction");
            Flat(sb, separator, healPerSecond, "HP/s");
            Percent(sb, separator, dodgeChance, "Dodge");
            Flat(sb, separator, startingShield, "Starting Shield");
            Percent(sb, separator, xpGainPercent, "XP Gain");
            Percent(sb, separator, goldBonusPercent, "Gold");
            Percent(sb, separator, ultimateChargePercent, "Ultimate Charge");
            return sb.Length == 0 ? "No stat changes" : sb.ToString();
        }

        private static void Percent(StringBuilder sb, string separator, float value, string label)
        {
            if (Mathf.Abs(value) < 0.0001f) return;
            if (sb.Length > 0) sb.Append(separator);
            sb.Append(value > 0f ? "+" : "-").Append((Mathf.Abs(value) * 100f).ToString("0.#")).Append("% ").Append(label);
        }

        private static void Flat(StringBuilder sb, string separator, float value, string label)
        {
            if (Mathf.Abs(value) < 0.0001f) return;
            if (sb.Length > 0) sb.Append(separator);
            sb.Append(value > 0f ? "+" : "-").Append(Mathf.Abs(value).ToString("0.##")).Append(' ').Append(label);
        }
    }
}
