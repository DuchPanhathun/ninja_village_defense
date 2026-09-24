using UnityEngine;

namespace NinjaVillage.Core.Combat
{
    /// <summary>Applies damage to every damageable in a circle — explosions, shockwaves, ultimates.</summary>
    public static class AreaDamage
    {
        private static readonly Collider2D[] Buffer = new Collider2D[64];

        // NOT readonly: SetLayerMask on a readonly struct field would mutate a defensive copy and the
        // mask would be ignored (explosions would then also hit the player, pickups, projectiles...).
        private static ContactFilter2D _filter = new() { useTriggers = true };

        /// <summary>
        /// Raised for every area-damage call that hit something (origin, radius), so a presentation
        /// layer can draw an explosion ring without this core class knowing about VFX.
        /// </summary>
        public static event System.Action<Vector2, float> AreaHit;

        /// <summary>Pushes every body in the circle away from <paramref name="origin"/> without damaging it (revive shockwave).</summary>
        public static int KnockbackCircle(Vector2 origin, float radius, LayerMask mask, float force)
        {
            _filter.SetLayerMask(mask);
            int count = Physics2D.OverlapCircle(origin, radius, _filter, Buffer);
            int pushed = 0;
            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (col == null || col.attachedRigidbody == null) continue;
                Vector2 direction = ((Vector2)col.transform.position - origin).normalized;
                if (direction == Vector2.zero) direction = Vector2.up;
                col.attachedRigidbody.AddForce(direction * force, ForceMode2D.Impulse);
                pushed++;
            }
            if (pushed > 0) AreaHit?.Invoke(origin, radius);
            return pushed;
        }

        /// <returns>Number of targets hit.</returns>
        public static int DamageCircle(Vector2 origin, float radius, LayerMask mask, float damage, float knockbackForce, GameObject source, bool isCritical = false)
        {
            _filter.SetLayerMask(mask);
            int count = Physics2D.OverlapCircle(origin, radius, _filter, Buffer);
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

            // Only when something was hit: some callers probe every frame until a target is in range.
            if (hits > 0) AreaHit?.Invoke(origin, radius);
            return hits;
        }

        /// <summary>
        /// Same as <see cref="DamageCircle"/> but also applies a status payload (burn/poison) and an
        /// optional slow/stun to everything hit — used by elemental evolutions (Firestorm, Thunder Kunai).
        /// </summary>
        public static int DamageCircleWithStatus(Vector2 origin, float radius, LayerMask mask, float damage, float knockbackForce, GameObject source,
            in StatusPayload status, float stunSeconds = 0f, bool isCritical = false)
        {
            _filter.SetLayerMask(mask);
            int count = Physics2D.OverlapCircle(origin, radius, _filter, Buffer);
            int hits = 0;

            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (col == null) continue;
                if (!col.TryGetComponent<IDamageable>(out var damageable) || !damageable.IsAlive) continue;

                Vector2 direction = ((Vector2)col.transform.position - origin).normalized;
                if (direction == Vector2.zero) direction = Vector2.up;

                damageable.TakeDamage(new DamageInfo(damage, isCritical, direction, knockbackForce, source));
                if (col.TryGetComponent<StatusEffectReceiver>(out var receiver))
                {
                    if (status.HasAny) receiver.Apply(status);
                    if (stunSeconds > 0f) receiver.ApplyStun(stunSeconds);
                }
                hits++;
            }

            // Only when something was hit: some callers probe every frame until a target is in range.
            if (hits > 0) AreaHit?.Invoke(origin, radius);
            return hits;
        }
    }
}
