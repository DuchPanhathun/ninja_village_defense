using NinjaVillage.Core.Combat;
using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Weapons
{
    /// <summary>Chain Sickle: no projectile — instantly hits the nearest target and yanks it toward the player.</summary>
    [CreateAssetMenu(fileName = "Weapon_ChainSickle", menuName = "Ninja Village/Weapons/Chain Sickle")]
    public class ChainSickleWeaponDefinition : WeaponDefinition
    {
        [Header("Chain Sickle - Pull")]
        [SerializeField] private float pullForce = 6f;

        public override string FireSoundId => NinjaVillage.Core.Audio.AudioCueIds.ChainSickleSwing;

        public override void Fire(AutoAttackController controller, Transform origin, Transform target, PlayerStats stats)
        {
            if (!target.TryGetComponent<IDamageable>(out var damageable) || !damageable.IsAlive) return;

            Vector2 originPos = origin.position;
            Vector2 pullDirection = (originPos - (Vector2)target.position).normalized; // toward the player, not away

            var (damage, isCritical) = controller.RollDamage();
            damageable.TakeDamage(new DamageInfo(damage, isCritical, pullDirection, pullForce, controller.gameObject));
            NinjaVillage.Gameplay.Vfx.Vfx.Slash(target.position, -pullDirection, 0.6f, new Color(0.8f, 0.8f, 0.9f));
        }
    }
}
