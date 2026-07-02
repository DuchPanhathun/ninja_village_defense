using UnityEngine;

namespace NinjaVillage.Core.Combat
{
    /// <summary>Applies damage to every damageable in a circle — explosions, shockwaves, ultimates.</summary>
    public static class AreaDamage
    {
        private static readonly Collider2D[] Buffer = new Collider2D[64];
        private static readonly ContactFilter2D Filter = new() { useTriggers = true };

        /// <returns>Number of targets hit.</returns>
        public static int DamageCircle(Vector2 origin, float radius, LayerMask mask, float damage, float knockbackForce, GameObject source, bool isCritical = false)
        {
            Filter.SetLayerMask(mask);
            int count = Physics2D.OverlapCircle(origin, radius, Filter, Buffer);
            int hits = 0;

            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (col == null) continue;
                if (!col.TryGetComponent<IDamageable>(out var damageable) || !damageable.IsAlive) continue;

                Vector2 direction = ((Vector2)col.transform.position - origin).normalized;
                if (direction == Vector2.zero) direction = Vector2.up;

                damageable.TakeDamage(new DamageInfo(damage, isCritical, direction, knockbackForce, source));
                hits++;
            }

            return hits;
        }
    }
}
