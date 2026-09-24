using NinjaVillage.Core.Combat;
using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Weapons
{
    /// <summary>Katana: no projectile — an instant melee swing that hits everything in an arc in front of the player.</summary>
    [CreateAssetMenu(fileName = "Weapon_Katana", menuName = "Ninja Village/Weapons/Katana")]
    public class KatanaWeaponDefinition : WeaponDefinition
    {
        [Header("Katana - Melee Arc")]
        [SerializeField] private float arcDegrees = 110f;

        public override string FireSoundId => NinjaVillage.Core.Audio.AudioCueIds.KatanaSlash;

        public override void Fire(AutoAttackController controller, Transform origin, Transform target, PlayerStats stats)
        {
            Vector2 originPos = origin.position;
            Vector2 facingDirection = ((Vector2)target.position - originPos).normalized;
            var (damage, isCritical) = controller.RollDamage();

            MeleeArc.DamageArc(originPos, facingDirection, Range, arcDegrees, controller.EnemyMask, damage, KnockbackForce, isCritical, controller.gameObject);

            // Slash animation (EPIC 2): a code-driven crescent along the swing, sized to the reach.
            NinjaVillage.Gameplay.Vfx.Vfx.Slash(originPos + facingDirection * (Range * 0.35f), facingDirection, Range * 0.6f);
        }
    }
}
