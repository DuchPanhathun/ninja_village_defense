using UnityEngine;

namespace NinjaVillage.Gameplay.World
{
    /// <summary>
    /// The "Obstacle" physics layer: solid scenery (trees, rocks, statues) in battle. It only collides with
    /// the Player and Enemy layers — projectiles, pickups and pets pass over it — and jumping characters
    /// exclude it (<see cref="Rigidbody2D.excludeLayers"/>) to hop over. The layer is added to the project by
    /// the Battle obstacles generator; without it everything falls back to Default and nothing is solid.
    /// </summary>
    public static class Obstacles
    {
        public const string LayerName = "Obstacle";

        public static int Layer { get; private set; } = -1;
        public static LayerMask Mask => Layer >= 0 ? 1 << Layer : 0;
        public static bool Available => Layer >= 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            Layer = LayerMask.NameToLayer(LayerName);
            if (Layer < 0) return;
            int player = LayerMask.NameToLayer("Player"), enemy = LayerMask.NameToLayer("Enemy");
            for (int other = 0; other < 32; other++)
                Physics2D.IgnoreLayerCollision(Layer, other, other != player && other != enemy);
        }
    }

    /// <summary>
    /// Steering around obstacles: probes ahead with a circle cast and, when something solid is in the way,
    /// slides along it, always to the same side (the caller keeps <c>side</c> until it has been clear for a
    /// while), so an enemy works its way round a corner instead of dithering at it.
    /// </summary>
    public static class ObstacleAvoidance
    {
        /// <param name="side">Remembered turning side (0 = none yet); pass the same field every call and reset
        /// it to 0 yourself once the way has been clear for a moment.</param>
        /// <returns>The direction to move in (<paramref name="desired"/> when nothing is in the way).</returns>
        public static Vector2 Steer(Vector2 position, Vector2 desired, float radius, float lookAhead, ref int side)
        {
            if (!Obstacles.Available || desired.sqrMagnitude < 0.0001f) return desired;
            var hit = Physics2D.CircleCast(position, radius, desired, lookAhead, Obstacles.Mask);
            if (hit.collider == null) return desired;
            Vector2 normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal : -desired;
            var tangent = new Vector2(-normal.y, normal.x);
            if (side == 0) side = Vector2.Dot(tangent, desired) >= 0f ? 1 : -1;
            // Mostly along the obstacle, a little away from it, never back the way we came.
            return (tangent * side * 0.9f + normal * 0.25f + desired * 0.1f).normalized;
        }
    }
}
