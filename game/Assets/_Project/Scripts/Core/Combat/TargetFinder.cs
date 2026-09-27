using System.Collections.Generic;
using NinjaVillage.Gameplay.Skills.Behaviors;
using UnityEngine;

namespace NinjaVillage.Core.Combat
{
    /// <summary>
    /// Finds damageable targets on a given layer — the heart of the auto-attack design.
    /// Every query reuses one static collider buffer, so auto-attacks, clones and chain
    /// lightning never allocate per frame. Dead targets (corpses still playing their death
    /// animation) and invisible targets are skipped.
    /// </summary>
    public static class TargetFinder
    {
        private static readonly Collider2D[] Buffer = new Collider2D[64];

        // NOT readonly: ContactFilter2D is a struct, and calling SetLayerMask on a readonly
        // field mutates a defensive copy — the mask would silently never be applied.
        private static ContactFilter2D _filter = new() { useTriggers = true };

        public static Transform FindNearest(Vector2 origin, float range, LayerMask mask) =>
            FindNearestExcluding(origin, range, mask, null, null);

        /// <summary>
        /// Nearest alive target, skipping <paramref name="exclude"/> and anything in
        /// <paramref name="excludeSet"/> (both optional) — used by chain lightning to hop to a new enemy.
        /// </summary>
        public static Transform FindNearestExcluding(Vector2 origin, float range, LayerMask mask, Transform exclude, HashSet<Transform> excludeSet)
        {
            int count = Overlap(origin, range, mask);
            if (count == 0) return null;

            Transform nearest = null;
            float nearestSqrDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (col == null) continue;

                var t = col.transform;
                if (exclude != null && t == exclude) continue;
                if (excludeSet != null && excludeSet.Contains(t)) continue;
                if (!IsValidTarget(col)) continue;

                float sqrDist = ((Vector2)t.position - origin).sqrMagnitude;
                if (sqrDist < nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearest = t;
                }
            }

            return nearest;
        }

        /// <summary>A random alive target in range (Lightning Strike), or null.</summary>
        public static Transform FindRandom(Vector2 origin, float range, LayerMask mask)
        {
            int count = Overlap(origin, range, mask);
            if (count == 0) return null;

            // Start at a random index and walk the buffer so a dead pick doesn't waste the roll.
            int start = Random.Range(0, count);
            for (int n = 0; n < count; n++)
            {
                var col = Buffer[(start + n) % count];
                if (col != null && IsValidTarget(col)) return col.transform;
            }
            return null;
        }

        /// <summary>Counts alive targets in range — cheap "am I surrounded?" checks.</summary>
        public static int CountAlive(Vector2 origin, float range, LayerMask mask)
        {
            int count = Overlap(origin, range, mask);
            int alive = 0;
            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (col != null && IsValidTarget(col)) alive++;
            }
            return alive;
        }

        /// <summary>
        /// Average position of alive targets in range (the "danger centre"), or false when none.
        /// Teleport-style skills blink directly away from it.
        /// </summary>
        public static bool TryGetThreatCenter(Vector2 origin, float range, LayerMask mask, out Vector2 center)
        {
            int count = Overlap(origin, range, mask);
            Vector2 sum = Vector2.zero;
            int alive = 0;
            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (col == null || !IsValidTarget(col)) continue;
                sum += (Vector2)col.transform.position;
                alive++;
            }

            center = alive > 0 ? sum / alive : origin;
            return alive > 0;
        }

        private static int Overlap(Vector2 origin, float range, LayerMask mask)
        {
            _filter.SetLayerMask(mask);
            return Physics2D.OverlapCircle(origin, range, _filter, Buffer);
        }

        private static bool IsValidTarget(Collider2D col)
        {
            // Corpses keep their collider while the death animation plays — never waste shots on them.
            if (col.TryGetComponent<IDamageable>(out var damageable) && !damageable.IsAlive) return false;
            // Skip targets that are invisible (Smoke Bomb skill).
            if (col.TryGetComponent<InvisibilityBehavior>(out var inv) && inv.IsInvisible) return false;
            return true;
        }
    }
}
