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

        private void Update()
        {
            if (_weapon == null) return;

            _cooldownRemaining -= Time.deltaTime;
            if (_cooldownRemaining > 0f) return;

            var target = TargetFinder.FindNearest(transform.position, _weapon.Definition.Range, enemyMask);
            if (target == null) return;

            Fire(target);

            float attacksPerSecond = _weapon.CurrentAttacksPerSecond * _stats.AttackSpeedMultiplier;
            _cooldownRemaining = 1f / Mathf.Max(0.01f, attacksPerSecond);
        }

        private void Fire(Transform target)
        {
            Vector2 origin = transform.position;
            Vector2 baseDirection = ((Vector2)target.position - origin).normalized;

            bool isCritical = Random.value < (_weapon.Definition.BaseCritChance + _stats.CritChanceBonus);
            float critMultiplier = isCritical ? _weapon.Definition.BaseCritMultiplier + _stats.CritMultiplierBonus : 1f;
            float damage = _weapon.CurrentDamage * _stats.AttackDamageMultiplier * critMultiplier;

            int totalProjectiles = 1 + Mathf.Max(0, _stats.ExtraProjectiles);
            float startAngleOffset = -(totalProjectiles - 1) * multiShotSpreadDegrees * 0.5f;

            for (int i = 0; i < totalProjectiles; i++)
            {
                float angle = startAngleOffset + i * multiShotSpreadDegrees;
                Vector2 direction = Quaternion.Euler(0, 0, angle) * baseDirection;
                SpawnProjectile(origin, direction, damage, isCritical);
            }
        }

        private void SpawnProjectile(Vector2 origin, Vector2 direction, float damage, bool isCritical)
        {
            if (_weapon.Definition.ProjectilePrefab == null)
            {
                Debug.LogWarning($"WeaponDefinition '{_weapon.Definition.DisplayName}' has no projectile prefab assigned.", this);
                return;
            }

            var instance = Instantiate(_weapon.Definition.ProjectilePrefab, origin, Quaternion.identity);
            var projectile = instance.GetComponent<Projectile>();
            projectile.Launch(direction, _weapon.Definition.ProjectileSpeed, damage, isCritical, _weapon.Definition.KnockbackForce, enemyMask, gameObject);
        }
    }
}
