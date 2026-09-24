using System.Collections;
using NinjaVillage.Core.Combat;
using UnityEngine;

namespace NinjaVillage.Gameplay.Ultimates
{
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
                var source = context.Runner.gameObject;
                var mask = context.EnemyMask;
                // Damage lands with the shuriken (a quarter second later), not before it.
                UltimateFx.FallingShuriken(context.Runner, burstCenter,
                    () => AreaDamage.DamageCircle(burstCenter, burstRadius, mask, damagePerBurst, 2f, source));
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
