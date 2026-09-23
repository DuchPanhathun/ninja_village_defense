using System.Collections.Generic;
using NinjaVillage.Core.Combat;
using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills;
using NinjaVillage.Gameplay.Ultimates;
using NinjaVillage.Gameplay.Weapons;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Meta
{
    /// <summary>
    /// Everything a meta-progression system may touch when a battle run starts.
    /// <see cref="IRunStartModifier"/>s push stat bonuses straight onto <see cref="Stats"/>
    /// and write the "one value wins" choices (weapon, ultimate, max-health math) into the
    /// fields below; <see cref="RunBootstrapper"/> applies those after every modifier ran,
    /// so the order modifiers run in doesn't matter for them.
    /// </summary>
    public sealed class RunStartContext
    {
        public SaveData Save;
        public GameObject Player;
        public PlayerStats Stats;
        public Health Health;
        public AutoAttackController AutoAttack;
        public UltimateController Ultimate;
        public SkillManager Skills;

        /// <summary>Max health from the player prefab's Health component, before bonuses.</summary>
        public float BaseMaxHealth;
        /// <summary>Additive: +0.1f = +10% max health. Final = (Base + Flat) * Multiplier.</summary>
        public float MaxHealthMultiplier = 1f;
        public float MaxHealthFlatBonus;
        /// <summary>Shield points the run starts with.</summary>
        public float StartingShield;

        /// <summary>Weapon to equip instead of the scene default (hero's signature weapon, Forge-equipped weapon...).</summary>
        public WeaponDefinition Weapon;
        public int WeaponLevel = 1;

        public UltimateDefinition UltimateOverride;

        /// <summary>Skills granted at level 1 before the first wave (hero passives, talents).</summary>
        public readonly List<SkillDefinition> StartingSkills = new();

        /// <summary>Selected hero id, for run records and analytics. Set by the Hero system.</summary>
        public string HeroId;

        /// <summary>Pet prefab to spawn next to the player. Set by the Pet system.</summary>
        public GameObject PetPrefab;
        /// <summary>Called once the pet is instantiated (pet system configures level/equipment here).</summary>
        public System.Action<GameObject> OnPetSpawned;

        public float FinalMaxHealth => Mathf.Max(1f, (BaseMaxHealth + MaxHealthFlatBonus) * MaxHealthMultiplier);
    }
}
