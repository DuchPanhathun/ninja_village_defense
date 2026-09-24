using System.Collections.Generic;
using UnityEngine;

namespace NinjaVillage.Gameplay.World
{
    public enum MinimapMarkerKind
    {
        Enemy,
        Elite,
        Boss,
        Chest,
        Item,
    }

    /// <summary>
    /// Puts this object on the battle minimap. Chests, equipment drops and bosses add one when they're
    /// created (<see cref="Add"/>); the minimap reads <see cref="Active"/>. Important kinds stay pinned to
    /// the map's edge when they're out of range, pointing the way.
    /// </summary>
    public class MinimapMarker : MonoBehaviour
    {
        private static readonly List<MinimapMarker> ActiveList = new();

        public static IReadOnlyList<MinimapMarker> Active => ActiveList;

        public MinimapMarkerKind Kind { get; private set; }

        /// <summary>Chests, items and bosses: shown even when out of range (clamped to the map edge).</summary>
        public bool PinToEdge => Kind == MinimapMarkerKind.Chest || Kind == MinimapMarkerKind.Item || Kind == MinimapMarkerKind.Boss;

        public static MinimapMarker Add(GameObject target, MinimapMarkerKind kind)
        {
            if (target == null) return null;
            if (!target.TryGetComponent<MinimapMarker>(out var marker)) marker = target.AddComponent<MinimapMarker>();
            marker.Kind = kind;
            return marker;
        }

        private void OnEnable() => ActiveList.Add(this);
        private void OnDisable() => ActiveList.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ActiveList.Clear();
    }
}
