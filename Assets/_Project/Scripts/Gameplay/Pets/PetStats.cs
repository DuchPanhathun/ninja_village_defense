using UnityEngine;

namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// Level-1 numbers + per-level growth for a pet, authored on <see cref="PetDefinition"/>.
    /// Deliberately generic ("power", "cooldown", "range", "effect duration") so every ability
    /// shares one leveling curve; each ability documents what the numbers mean for it.
    /// Public fields keep it a plain serializable struct that tests can build directly.
    /// </summary>
    [System.Serializable]
    public struct PetStatTemplate
    {
        [Tooltip("Damage (Wolf/Monkey/Dragon), pull speed (Fox) or treasure coin value (Hawk) at level 1.")]
        public float basePower;
        [Tooltip("Multiplicative growth per level above 1: 0.08 = +8% power per level.")]
        public float powerGrowthPerLevel;
        [Tooltip("Seconds between ability uses at level 1.")]
        public float baseCooldown;
        [Tooltip("Cooldown shrinks by this fraction per level (floored at PetStatMath.MinCooldownFraction).")]
        public float cooldownReductionPerLevel;
        [Tooltip("Aggro / throw / breath / pull radius at level 1 (world units).")]
        public float baseRange;
        public float rangePerLevel;
        [Tooltip("Stun (Monkey) or burn (Dragon) seconds at level 1.")]
        public float baseEffectDuration;
        public float effectDurationPerLevel;
        [Tooltip("Max follow / chase speed (units per second).")]
        public float moveSpeed;

        public static PetStatTemplate Default => new PetStatTemplate
        {
            basePower = 10f,
            powerGrowthPerLevel = 0.08f,
            baseCooldown = 2f,
            cooldownReductionPerLevel = 0.02f,
            baseRange = 5f,
            rangePerLevel = 0.1f,
            baseEffectDuration = 1f,
            effectDurationPerLevel = 0.05f,
            moveSpeed = 7f
        };
    }

    /// <summary>Stat bonuses from one piece of pet gear (EPIC 12 "Pet equipment"). Summed across gear.</summary>
    [System.Serializable]
    public struct PetEquipmentBonus
    {
        [Tooltip("0.15 = +15% power.")]
        public float powerPercent;
        [Tooltip("0.1 = 10% shorter cooldown (total capped at PetStatMath.MaxEquipmentCooldownReduction).")]
        public float cooldownReduction;
        [Tooltip("Flat range added (world units).")]
        public float rangeFlat;
        [Tooltip("0.2 = +20% stun/burn duration.")]
        public float durationPercent;
        [Tooltip("0.1 = +10% move speed.")]
        public float moveSpeedPercent;

        public static PetEquipmentBonus operator +(PetEquipmentBonus a, PetEquipmentBonus b) => new PetEquipmentBonus
        {
            powerPercent = a.powerPercent + b.powerPercent,
            cooldownReduction = a.cooldownReduction + b.cooldownReduction,
            rangeFlat = a.rangeFlat + b.rangeFlat,
            durationPercent = a.durationPercent + b.durationPercent,
            moveSpeedPercent = a.moveSpeedPercent + b.moveSpeedPercent
        };

        public bool IsEmpty =>
            powerPercent == 0f && cooldownReduction == 0f && rangeFlat == 0f && durationPercent == 0f && moveSpeedPercent == 0f;

        /// <summary>"+15% power, -10% cooldown" for UI.</summary>
        public string Describe()
        {
            var sb = new System.Text.StringBuilder();
            AppendPart(sb, powerPercent, "power", true);
            AppendPart(sb, -cooldownReduction, "cooldown", true);
            AppendPart(sb, rangeFlat, "range", false);
            AppendPart(sb, durationPercent, "effect duration", true);
            AppendPart(sb, moveSpeedPercent, "speed", true);
            return sb.Length == 0 ? "No bonus" : sb.ToString();
        }

        private static void AppendPart(System.Text.StringBuilder sb, float value, string label, bool percent)
        {
            if (Mathf.Approximately(value, 0f)) return;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(value > 0f ? "+" : "-");
            sb.Append(percent ? (Mathf.Abs(value) * 100f).ToString("0.#") + "%" : Mathf.Abs(value).ToString("0.##"));
            sb.Append(' ').Append(label);
        }
    }

    /// <summary>
    /// Passive bonus the active pet gives its owner for the whole run (e.g. the Hawk's luck).
    /// Additive onto <c>PlayerStats</c> multipliers: 0.05 = +5%.
    /// </summary>
    [System.Serializable]
    public struct PetOwnerBonus
    {
        public float xpGainPercent;
        public float luckyDropChance;
        public float goldBonusPercent;
        public float xpMagnetPercent;
        public float moveSpeedPercent;

        /// <summary>base + perLevel × (level − 1).</summary>
        public static PetOwnerBonus AtLevel(in PetOwnerBonus baseBonus, in PetOwnerBonus perLevel, int level)
        {
            float steps = Mathf.Max(0, level - 1);
            return new PetOwnerBonus
            {
                xpGainPercent = baseBonus.xpGainPercent + perLevel.xpGainPercent * steps,
                luckyDropChance = baseBonus.luckyDropChance + perLevel.luckyDropChance * steps,
                goldBonusPercent = baseBonus.goldBonusPercent + perLevel.goldBonusPercent * steps,
                xpMagnetPercent = baseBonus.xpMagnetPercent + perLevel.xpMagnetPercent * steps,
                moveSpeedPercent = baseBonus.moveSpeedPercent + perLevel.moveSpeedPercent * steps
            };
        }

        public bool IsEmpty =>
            xpGainPercent == 0f && luckyDropChance == 0f && goldBonusPercent == 0f && xpMagnetPercent == 0f && moveSpeedPercent == 0f;

        /// <summary>"+5% XP, +3% lucky drop" for UI.</summary>
        public string Describe()
        {
            var sb = new System.Text.StringBuilder();
            Append(sb, xpGainPercent, "XP gain");
            Append(sb, luckyDropChance, "lucky drop");
            Append(sb, goldBonusPercent, "gold");
            Append(sb, xpMagnetPercent, "pickup radius");
            Append(sb, moveSpeedPercent, "move speed");
            return sb.ToString();
        }

        private static void Append(System.Text.StringBuilder sb, float value, string label)
        {
            if (Mathf.Approximately(value, 0f)) return;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(value > 0f ? "+" : "-").Append((Mathf.Abs(value) * 100f).ToString("0.#")).Append("% ").Append(label);
        }
    }

    /// <summary>The resolved numbers a spawned pet's ability actually uses (level + gear applied).</summary>
    public struct PetRuntimeStats
    {
        public float Power;
        public float Cooldown;
        public float Range;
        public float EffectDuration;
        public float MoveSpeed;
    }

    /// <summary>
    /// Pet level scaling (EPIC 12 "Pet leveling") as pure functions so balance is unit-tested:
    /// power grows multiplicatively, cooldown shrinks linearly to a floor, range/duration grow
    /// linearly, then gear bonuses apply on top.
    /// </summary>
    public static class PetStatMath
    {
        /// <summary>Cooldown never drops below this fraction of the level-1 cooldown from leveling.</summary>
        public const float MinCooldownFraction = 0.35f;
        /// <summary>Total gear cooldown reduction cap.</summary>
        public const float MaxEquipmentCooldownReduction = 0.5f;
        public const float MinCooldownSeconds = 0.1f;

        public static float ScalePower(float basePower, float growthPerLevel, int level) =>
            basePower * (1f + growthPerLevel * Mathf.Max(0, level - 1));

        public static float ScaleCooldown(float baseCooldown, float reductionPerLevel, int level) =>
            baseCooldown * Mathf.Max(MinCooldownFraction, 1f - reductionPerLevel * Mathf.Max(0, level - 1));

        public static float ScaleLinear(float baseValue, float perLevel, int level) =>
            baseValue + perLevel * Mathf.Max(0, level - 1);

        public static PetRuntimeStats Build(in PetStatTemplate template, int level, in PetEquipmentBonus gear)
        {
            level = Mathf.Max(1, level);
            float gearCooldown = Mathf.Clamp(gear.cooldownReduction, 0f, MaxEquipmentCooldownReduction);

            return new PetRuntimeStats
            {
                Power = ScalePower(template.basePower, template.powerGrowthPerLevel, level) * (1f + gear.powerPercent),
                Cooldown = Mathf.Max(MinCooldownSeconds,
                    ScaleCooldown(template.baseCooldown, template.cooldownReductionPerLevel, level) * (1f - gearCooldown)),
                Range = Mathf.Max(0.5f, ScaleLinear(template.baseRange, template.rangePerLevel, level) + gear.rangeFlat),
                EffectDuration = Mathf.Max(0f, ScaleLinear(template.baseEffectDuration, template.effectDurationPerLevel, level) * (1f + gear.durationPercent)),
                MoveSpeed = Mathf.Max(0.5f, template.moveSpeed * (1f + gear.moveSpeedPercent))
            };
        }

        /// <summary>
        /// Pets inherit part of the owner's bonus damage so they stay relevant as enemy HP scales:
        /// with a 1.6× owner multiplier and 50% inheritance a pet deals 1.3× its power.
        /// </summary>
        public static float InheritOwnerDamage(float power, float ownerDamageMultiplier, float inheritFraction) =>
            power * Mathf.Max(0f, 1f + (ownerDamageMultiplier - 1f) * inheritFraction);
    }
}
