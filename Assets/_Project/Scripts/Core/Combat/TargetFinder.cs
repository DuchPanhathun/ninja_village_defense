using NinjaVillage.Gameplay.Skills.Behaviors;
using UnityEngine;

namespace NinjaVillage.Core.Combat
{
    /// <summary>Finds the nearest damageable target on a given layer — the heart of the auto-attack design.</summary>
    public static class TargetFinder
    {
        private static readonly Collider2D[] Buffer = new Collider2D[64];
        private static readonly ContactFilter2D Filter = new() { useTriggers = true };

        public static Transform FindNearest(Vector2 origin, float range, LayerMask mask)
        {
            Filter.SetLayerMask(mask);
            int count = Physics2D.OverlapCircle(origin, range, Filter, Buffer);
            if (count == 0) return null;

            Transform nearest = null;
            float nearestSqrDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var col = Buffer[i];
                if (col == null) continue;

                // Skip targets that are invisible (Smoke Bomb skill).
                if (col.TryGetComponent<InvisibilityBehavior>(out var inv) && inv.IsInvisible) continue;

                float sqrDist = ((Vector2)col.transform.position - origin).sqrMagnitude;
                if (sqrDist < nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearest = col.transform;
                }
            }

            return nearest;
        }
    }
}
