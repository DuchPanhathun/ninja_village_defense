using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Weapons
{
    /// <summary>
    /// Shuriken: instead of aiming at one target, throws a full circle of shuriken
    /// around the player every attack. Enable piercing by setting Pierce Count &gt; 0
    /// directly on this weapon's projectile prefab — Projectile already supports it.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_Shuriken", menuName = "Ninja Village/Weapons/Shuriken")]
    public class ShurikenWeaponDefinition : WeaponDefinition
    {
        [Header("Shuriken - Circular Throw")]
        [SerializeField] private int baseProjectileCount = 8;

        public override void Fire(AutoAttackController controller, Transform origin, Transform target, PlayerStats stats)
        {
            Vector2 originPos = origin.position;
            var (damage, isCritical) = controller.RollDamage();

            int count = baseProjectileCount + Mathf.Max(0, stats.ExtraProjectiles);
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                Vector2 direction = Quaternion.Euler(0, 0, i * angleStep) * Vector2.right;
                controller.SpawnProjectile(originPos, direction, damage, isCritical, KnockbackForce);
            }
        }
    }
}
