using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Utilities;
using UnityEngine;

namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// Allocation-free enemy queries shared by pet abilities (nearest target, circle/cone hits).
    /// Builds its ContactFilter2D per call (a local struct) so the layer mask is really applied,
    /// and double-checks the layer on every result.
    /// </summary>
    public static class PetCombat
    {
        private static readonly Collider2D[] Buffer = new Collider2D[64];

        /// <summary>Falls back to the "Enemy" layer when a mask isn't configured.</summary>
        public static LayerMask ResolveEnemyMask(LayerMask mask)
        {
            if (mask.value != 0) return mask;
            int fallback = LayerMask.GetMask(Layers.Enemy);
            return fallback;
        }

        /// <summary>Fills the shared buffer; read results with <see cref="GetResult"/> before the next query.</summary>
        public static int OverlapEnemies(Vector2 origin, float radius, LayerMask mask)
        {
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(mask);
            return Physics2D.OverlapCircle(origin, radius, filter, Buffer);
        }

        public static Collider2D GetResult(int index) => Buffer[index];

        /// <summary>True when the collider is on <paramref name="mask"/> and has a living damageable.</summary>
        public static bool TryGetLivingTarget(Collider2D collider, LayerMask mask, out IDamageable target)
        {
            target = null;
            if (collider == null) return false;
            if (((1 << collider.gameObject.layer) & mask.value) == 0) return false;
            if (!collider.TryGetComponent<IDamageable>(out target)) return false;
            return target.IsAlive;
        }

        /// <summary>Nearest living enemy within <paramref name="radius"/> of <paramref name="origin"/>, or null.</summary>
        public static Transform FindNearestEnemy(Vector2 origin, float radius, LayerMask mask)
        {
            int count = OverlapEnemies(origin, radius, mask);
            Transform nearest = null;
            float best = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (!TryGetLivingTarget(col, mask, out _)) continue;

                float sqr = ((Vector2)col.transform.position - origin).sqrMagnitude;
                if (sqr < best)
                {
                    best = sqr;
                    nearest = col.transform;
                }
            }
            return nearest;
        }

        /// <summary>
        /// Damages every living enemy in a circle (optionally limited to a cone around
        /// <paramref name="coneDirection"/>) and applies optional stun/burn. Returns hits.
        /// </summary>
        public static int HitArea(Vector2 origin, float radius, LayerMask mask, float damage, float knockback, GameObject source,
            Vector2 coneDirection = default, float coneHalfAngle = 180f, float stunSeconds = 0f, float burnDps = 0f, float burnSeconds = 0f)
        {
            int count = OverlapEnemies(origin, radius, mask);
            int hits = 0;
            bool useCone = coneHalfAngle < 180f && coneDirection.sqrMagnitude > 0.0001f;

            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (!TryGetLivingTarget(col, mask, out var target)) continue;

                Vector2 toTarget = (Vector2)col.transform.position - origin;
                if (useCone && toTarget.sqrMagnitude > 0.04f && Vector2.Angle(coneDirection, toTarget) > coneHalfAngle)
                    continue;

                Vector2 direction = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.up;
                if (damage > 0f)
                    target.TakeDamage(new DamageInfo(damage, false, direction, knockback, source));

                if ((stunSeconds > 0f || burnDps > 0f) && col.TryGetComponent<StatusEffectReceiver>(out var status))
                {
                    if (stunSeconds > 0f) status.ApplyStun(stunSeconds);
                    if (burnDps > 0f && burnSeconds > 0f) status.ApplyBurn(burnDps, burnSeconds);
                }
                hits++;
            }
            return hits;
        }
    }
}
