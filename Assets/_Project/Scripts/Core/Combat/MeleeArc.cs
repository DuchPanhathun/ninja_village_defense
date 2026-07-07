using UnityEngine;

namespace NinjaVillage.Core.Combat
{
    /// <summary>Damages every damageable within a radius AND within an angular arc facing a direction — melee sword swings.</summary>
    public static class MeleeArc
    {
        private static readonly Collider2D[] Buffer = new Collider2D[32];
        private static readonly ContactFilter2D Filter = new() { useTriggers = true };

        /// <returns>Number of targets hit.</returns>
        public static int DamageArc(Vector2 origin, Vector2 facingDirection, float radius, float arcDegrees, LayerMask mask, float damage, float knockbackForce, bool isCritical, GameObject source)
        {
            Filter.SetLayerMask(mask);
            int count = Physics2D.OverlapCircle(origin, radius, Filter, Buffer);
            int hits = 0;
            float halfArc = arcDegrees * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (col == null) continue;

                Vector2 toTarget = (Vector2)col.transform.position - origin;
                if (toTarget.sqrMagnitude < 0.0001f) continue;
                if (Vector2.Angle(facingDirection, toTarget) > halfArc) continue;

                if (!col.TryGetComponent<IDamageable>(out var damageable) || !damageable.IsAlive) continue;

                damageable.TakeDamage(new DamageInfo(damage, isCritical, toTarget.normalized, knockbackForce, source));
                hits++;
            }

            return hits;
        }
    }
}
