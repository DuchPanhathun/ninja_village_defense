using NinjaVillage.Core;
using NinjaVillage.Core.ScriptableObjects;
using UnityEngine;

namespace NinjaVillage.Gameplay.Weapons
{
    /// <summary>
    /// Data definition for a weapon. Every weapon (Kunai, Shuriken, Katana, Bow,
    /// Chain Sickle, ...) is an instance of this asset — behavior differences
    /// beyond "throw a projectile at the nearest enemy" live in a dedicated
    /// controller (see <see cref="Combat.AutoAttackController"/> for the shared
    /// projectile path).
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Ninja Village/Weapon Definition")]
    public class WeaponDefinition : DescriptiveScriptableObject
    {
        [Header("Rarity")]
        [SerializeField] private Rarity rarity = Rarity.Common;

        [Header("Base Stats (level 1)")]
        [SerializeField] private float baseDamage = 10f;
        [SerializeField] private float baseAttacksPerSecond = 1f;
        [SerializeField] private float range = 6f;
        [SerializeField] private float baseCritChance = 0.05f;
        [SerializeField] private float baseCritMultiplier = 1.5f;
        [SerializeField] private float knockbackForce = 2f;

        [Header("Per-Level Growth")]
        [SerializeField] private float damagePerLevel = 2f;
        [SerializeField] private float attacksPerSecondPerLevel = 0.05f;

        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 12f;

        public Rarity Rarity => rarity;
        public float Range => range;
        public float BaseCritChance => baseCritChance;
        public float BaseCritMultiplier => baseCritMultiplier;
        public float KnockbackForce => knockbackForce;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => projectileSpeed;

        public float GetDamage(int level) => baseDamage + damagePerLevel * (level - 1);
        public float GetAttacksPerSecond(int level) => baseAttacksPerSecond + attacksPerSecondPerLevel * (level - 1);
    }
}
