using System.Collections;
using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Gameplay.Ultimates
{
    // ---------------------------------------------------------------------
    // Two of the three design-doc ultimates. Shadow Clone Army needs a clone
    // prefab + clone AI (mini auto-attackers), so it waits until prefabs exist.
    // ---------------------------------------------------------------------

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

    /// <summary>"Rain of 500 shurikens." — a storm of damage bursts around the player over several seconds.</summary>
    [CreateAssetMenu(fileName = "Ultimate_HeavenlyStorm", menuName = "Ninja Village/Ultimates/Heavenly Storm")]
    public class HeavenlyStormUltimate : UltimateDefinition
    {
        [SerializeField] private int burstCount = 50;
        [SerializeField] private float damagePerBurst = 20f;
        [SerializeField] private float burstRadius = 1.2f;
        [SerializeField] private float stormRadius = 8f;
        [SerializeField] private float duration = 5f;

        public override void Activate(in UltimateContext context)
        {
            context.Runner.StartCoroutine(StormRoutine(context));
        }

        private IEnumerator StormRoutine(UltimateContext context)
        {
            float interval = duration / burstCount;

            for (int i = 0; i < burstCount; i++)
            {
                if (context.PlayerTransform == null) yield break;

                Vector2 burstCenter = (Vector2)context.PlayerTransform.position + Random.insideUnitCircle * stormRadius;
                AreaDamage.DamageCircle(burstCenter, burstRadius, context.EnemyMask, damagePerBurst, 2f, context.Runner.gameObject);

                // TODO(VFX): falling shuriken + impact effect at burstCenter (EPIC 23).
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
