using NinjaVillage.Core.Combat;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Weapons;
using UnityEngine;

namespace NinjaVillage.Gameplay.Combat
{
    /// <summary>
    /// The heart of the "player only moves" design: every frame, find the nearest
    /// enemy in range and auto-fire the equipped weapon at it on a cooldown.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class AutoAttackController : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition startingWeapon;
        [SerializeField] private LayerMask enemyMask;
        [Tooltip("Degrees between shots when ExtraProjectiles > 0.")]
        [SerializeField] private float multiShotSpreadDegrees = 12f;

        private PlayerStats _stats;
        private RuntimeWeapon _weapon;
        private float _cooldownRemaining;

        public RuntimeWeapon Weapon => _weapon;
        /// <summary>Shared by behavior skills (Lightning Strike, Explosive Bomb) and ultimates so the enemy layer is configured once.</summary>
        public LayerMask EnemyMask => enemyMask;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            if (startingWeapon != null)
                EquipWeapon(startingWeapon);
        }

        public void EquipWeapon(WeaponDefinition definition, int level = 1)
        {
            _weapon = new RuntimeWeapon(definition, level);
        }

        /// <summary>Degrees between shots when firing more than one projectile at once (multi-shot skills or an innately multi-projectile weapon).</summary>
        public float MultiShotSpreadDegrees => multiShotSpreadDegrees;
        public PlayerStats Stats => _stats;

        private void Update()
        {
            if (_weapon == null) return;

            _cooldownRemaining -= Time.deltaTime;
            if (_cooldownRemaining > 0f) return;

            var target = TargetFinder.FindNearest(transform.position, _weapon.Definition.Range, enemyMask);
            if (target == null) return;

            _weapon.Definition.Fire(this, transform, target, _stats);

            float attacksPerSecond = _weapon.CurrentAttacksPerSecond * _stats.AttackSpeedMultiplier;
            _cooldownRemaining = 1f / Mathf.Max(0.01f, attacksPerSecond);
        }

        /// <summary>Rolls a crit and returns the final damage for this weapon's current level — shared by every weapon's Fire() override.</summary>
        public (float damage, bool isCritical) RollDamage()
        {
            bool isCritical = Random.value < (_weapon.Definition.BaseCritChance + _stats.CritChanceBonus);
            float critMultiplier = isCritical ? _weapon.Definition.BaseCritMultiplier + _stats.CritMultiplierBonus : 1f;
            float damage = _weapon.CurrentDamage * _stats.AttackDamageMultiplier * critMultiplier;
            return (damage, isCritical);
        }

        /// <summary>Spawns and launches one projectile from the equipped weapon's prefab — reusable by any WeaponDefinition.Fire() override.</summary>
        public void SpawnProjectile(Vector2 origin, Vector2 direction, float damage, bool isCritical, float knockbackForce, float speedOverride = -1f)
        {
            if (_weapon.Definition.ProjectilePrefab == null)
            {
                Debug.LogWarning($"WeaponDefinition '{_weapon.Definition.DisplayName}' has no projectile prefab assigned.", this);
                return;
            }

            float speed = speedOverride > 0f ? speedOverride : _weapon.Definition.ProjectileSpeed;
            var instance = Instantiate(_weapon.Definition.ProjectilePrefab, origin, Quaternion.identity);
            if (!Mathf.Approximately(_stats.ProjectileSizeMultiplier, 1f))
                instance.transform.localScale *= _stats.ProjectileSizeMultiplier;

            var projectile = instance.GetComponent<Projectile>();
            projectile.Launch(direction, speed, damage, isCritical, knockbackForce, enemyMask, gameObject);

            if (_stats.BurnOnHitDps > 0f || _stats.PoisonOnHitDps > 0f)
            {
                projectile.SetStatusPayload(new StatusPayload
                {
                    BurnDps = _stats.BurnOnHitDps,
                    BurnDuration = _stats.BurnOnHitDuration,
                    PoisonDps = _stats.PoisonOnHitDps,
                    PoisonDuration = _stats.PoisonOnHitDuration
                });
            }
        }

        /// <summary>Fires <paramref name="totalProjectiles"/> copies spread evenly around <paramref name="baseDirection"/> — the standard multi-shot fan used by ranged weapons.</summary>
        public void SpawnProjectileFan(Vector2 origin, Vector2 baseDirection, int totalProjectiles, float damage, bool isCritical, float knockbackForce)
        {
            float startAngleOffset = -(totalProjectiles - 1) * multiShotSpreadDegrees * 0.5f;
            for (int i = 0; i < totalProjectiles; i++)
            {
                float angle = startAngleOffset + i * multiShotSpreadDegrees;
                Vector2 direction = Quaternion.Euler(0, 0, angle) * baseDirection;
                SpawnProjectile(origin, direction, damage, isCritical, knockbackForce);
            }
        }
    }
}
