using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Weapons
{
    /// <summary>
    /// Bow: long range (tune via the base Range field to a high value on the asset).
    /// Every Nth shot is an automatic "charged" shot with bonus damage and speed —
    /// since the design has no manual attack input, charge is time/shot-based
    /// instead of a held button.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_Bow", menuName = "Ninja Village/Weapons/Bow")]
    public class BowWeaponDefinition : WeaponDefinition
    {
        [Header("Bow - Charge Shot")]
        [SerializeField] private int shotsPerCharge = 3;
        [SerializeField] private float chargedDamageMultiplier = 2.5f;
        [SerializeField] private float chargedSpeedMultiplier = 1.5f;

        public override string FireSoundId => NinjaVillage.Core.Audio.AudioCueIds.BowShot;

        public override void Fire(AutoAttackController controller, Transform origin, Transform target, PlayerStats stats)
        {
            Vector2 originPos = origin.position;
            Vector2 direction = ((Vector2)target.position - originPos).normalized;
            var (damage, isCritical) = controller.RollDamage();

            var weapon = controller.Weapon; // per-equip runtime state, safe to mutate
            weapon.FireCount++;
            bool isCharged = weapon.FireCount >= shotsPerCharge;
            if (isCharged)
            {
                weapon.FireCount = 0;
                damage *= chargedDamageMultiplier;
            }

            float speed = isCharged ? ProjectileSpeed * chargedSpeedMultiplier : -1f;
            controller.SpawnProjectile(originPos, direction, damage, isCritical || isCharged, KnockbackForce, speed);
        }
    }
}
