using NinjaVillage.Core;
using NinjaVillage.Core.Audio;
using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Weapons
{
    /// <summary>
    /// Data definition for a weapon. Every weapon (Kunai, Shuriken, Katana, Bow,
    /// Chain Sickle, ...) is an instance of this asset. <see cref="Fire"/> is the
    /// extension point for weapon-specific attack patterns — the default here is
    /// the basic "throw N projectiles at the nearest enemy" behavior (Kunai);
    /// other weapons subclass this and override it.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Ninja Village/Weapon Definition")]
    public class WeaponDefinition : DescriptiveScriptableObject, IWeapon
    {
        [Header("Rarity")]
        [SerializeField] private Rarity rarity = Rarity.Common;
        [SerializeField] private Element element = Element.None;

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
        public Element Element => element;
        public float Range => range;
        public float BaseCritChance => baseCritChance;
        public float BaseCritMultiplier => baseCritMultiplier;
        public float KnockbackForce => knockbackForce;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileSpeed => projectileSpeed;

        /// <summary>Sound played each time this weapon fires (AutoAttackController plays it).</summary>
        public virtual string FireSoundId => AudioCueIds.KunaiThrow;

        public float GetDamage(int level) => baseDamage + damagePerLevel * (level - 1);
        public float GetAttacksPerSecond(int level) => baseAttacksPerSecond + attacksPerSecondPerLevel * (level - 1);

        /// <summary>
        /// Fires this weapon once. Default behavior: throw a fan of projectiles
        /// (1 + PlayerStats.ExtraProjectiles) straight at <paramref name="target"/>.
        /// Override for weapons with a different attack pattern (melee arcs,
        /// circular bursts, pull effects, ...).
        /// </summary>
        public virtual void Fire(AutoAttackController controller, Transform origin, Transform target, PlayerStats stats)
        {
            Vector2 originPos = origin.position;
            Vector2 baseDirection = ((Vector2)target.position - originPos).normalized;

            var (damage, isCritical) = controller.RollDamage();
            int totalProjectiles = 1 + Mathf.Max(0, stats.ExtraProjectiles);

            controller.SpawnProjectileFan(originPos, baseDirection, totalProjectiles, damage, isCritical, KnockbackForce);
        }
    }
}
