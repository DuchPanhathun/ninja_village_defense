using System.Collections.Generic;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    public enum PlacementBlocker
    {
        None,
        OutsideVillage,
        OnBuilding,
        OnDecoration,
        OnDisplay,
    }

    /// <summary>
    /// Pure placement rules (unit-testable): a decoration must sit inside the village, off building plots,
    /// clear of the displays and not overlapping another decoration. Positions snap to a half-unit grid.
    /// </summary>
    public static class DecorationRules
    {
        public const float Grid = 0.5f;
        /// <summary>Selling returns this share of the price.</summary>
        public const float RefundShare = 0.5f;

        public static Vector2 Snap(Vector2 position) =>
            new(Mathf.Round(position.x / Grid) * Grid, Mathf.Round(position.y / Grid) * Grid);

        public static int Refund(int price) => Mathf.FloorToInt(price * RefundShare);

        /// <param name="plots">Building plots as rects (world space).</param>
        /// <param name="others">Other placed decorations (position, radius), excluding the one being moved.</param>
        public static PlacementBlocker Check(Vector2 position, float radius, Rect bounds, IEnumerable<Rect> plots,
            IEnumerable<(Vector2 center, float radius)> reserved, IEnumerable<(Vector2 center, float radius)> others)
        {
            if (position.x - radius < bounds.xMin || position.x + radius > bounds.xMax ||
                position.y - radius < bounds.yMin || position.y + radius > bounds.yMax)
                return PlacementBlocker.OutsideVillage;

            if (plots != null)
                foreach (var plot in plots)
                    if (CircleOverlapsRect(position, radius, plot)) return PlacementBlocker.OnBuilding;

            if (reserved != null)
                foreach (var (center, r) in reserved)
                    if ((position - center).sqrMagnitude < (radius + r) * (radius + r)) return PlacementBlocker.OnDisplay;

            if (others != null)
                foreach (var (center, r) in others)
                    if ((position - center).sqrMagnitude < (radius + r) * (radius + r) * 0.8f) return PlacementBlocker.OnDecoration;

            return PlacementBlocker.None;
        }

        public static bool CircleOverlapsRect(Vector2 center, float radius, Rect rect)
        {
            float x = Mathf.Clamp(center.x, rect.xMin, rect.xMax);
            float y = Mathf.Clamp(center.y, rect.yMin, rect.yMax);
            return (center - new Vector2(x, y)).sqrMagnitude < radius * radius;
        }

        public static string Describe(PlacementBlocker blocker) => blocker switch
        {
            PlacementBlocker.OutsideVillage => "Keep it inside the village.",
            PlacementBlocker.OnBuilding => "That spot is taken by a building.",
            PlacementBlocker.OnDecoration => "Too close to another decoration.",
            PlacementBlocker.OnDisplay => "That spot belongs to a village display.",
            _ => string.Empty,
        };
    }
}
