using System;

namespace NinjaVillage.Gameplay.Weapons
{
    /// <summary>
    /// The equipped, levelable instance of a <see cref="WeaponDefinition"/>. The
    /// definition asset is shared, immutable data; this class is the small bit
    /// of per-save mutable state (current level) layered on top of it.
    /// </summary>
    [Serializable]
    public class RuntimeWeapon
    {
        public WeaponDefinition Definition { get; }
        public int Level { get; private set; }

        /// <summary>Scratch counter for weapons with "every Nth shot" patterns (e.g. Bow's charge shot). Lives here, not on the shared WeaponDefinition asset, since this is per-equip runtime state.</summary>
        public int FireCount { get; set; }

        public const int MaxLevel = 10;

        public RuntimeWeapon(WeaponDefinition definition, int level = 1)
        {
            Definition = definition;
            Level = Math.Clamp(level, 1, MaxLevel);
        }

        public bool CanUpgrade => Level < MaxLevel;

        public void Upgrade()
        {
            if (CanUpgrade) Level++;
        }

        public float CurrentDamage => Definition.GetDamage(Level);
        public float CurrentAttacksPerSecond => Definition.GetAttacksPerSecond(Level);
    }
}
