using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Gameplay.Ultimates
{
    /// <summary>"Huge dragon crosses the map. Kills everything." — massive damage to every enemy on screen.</summary>
    [CreateAssetMenu(fileName = "Ultimate_DragonSlash", menuName = "Ninja Village/Ultimates/Dragon Slash")]
    public class DragonSlashUltimate : UltimateDefinition
    {
        [SerializeField] private float damage = 500f;
        [Tooltip("Covers everything on and around the screen.")]
        [SerializeField] private float radius = 20f;

        public override void Activate(in UltimateContext context)
        {
            AreaDamage.DamageCircle(context.PlayerTransform.position, radius, context.EnemyMask, damage, 10f, context.Runner.gameObject, isCritical: true);
            EventBus<CameraShakeRequestEvent>.Raise(new CameraShakeRequestEvent(0.6f, 0.4f));

            // TODO(VFX): dragon sweep animation across the map (EPIC 23).
        }
    }
}
