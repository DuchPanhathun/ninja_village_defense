using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>Anything in the village world the player can tap (buildings, villagers).</summary>
    public interface IVillageTappable
    {
        void OnTapped();
    }

    /// <summary>
    /// Something a finger can drag around the map instead of panning the camera (the decoration being
    /// placed). The camera controller checks for one under the finger when a press starts.
    /// </summary>
    public interface IVillageDraggable
    {
        bool CanDrag { get; }
        void BeginDrag(Vector2 world);
        void Drag(Vector2 world);
        void EndDrag();
    }

    /// <summary>Raised when the player taps a building on the village map; the village HUD opens its menu.</summary>
    public readonly struct VillageBuildingTappedEvent : IGameEvent
    {
        public readonly string BuildingId;
        public VillageBuildingTappedEvent(string buildingId) => BuildingId = buildingId;
    }

    /// <summary>What a village display stands for — the HUD opens the matching screen.</summary>
    public enum VillageDisplayKind
    {
        Heroes,
        Pets,
        Gear,
        Talents,
        Profile,
        Storehouse,
    }

    /// <summary>A hero, pet, the Armory rack or the Talent Tree was tapped.</summary>
    public readonly struct VillageDisplayTappedEvent : IGameEvent
    {
        public readonly VillageDisplayKind Kind;
        public VillageDisplayTappedEvent(VillageDisplayKind kind) => Kind = kind;
    }

    /// <summary>A placed decoration was tapped (own village only): the HUD offers Move / Sell.</summary>
    public readonly struct DecorationTappedEvent : IGameEvent
    {
        public readonly int Uid;
        public DecorationTappedEvent(int uid) => Uid = uid;
    }

    /// <summary>A villager with a request was tapped (own village): the HUD opens the requests, that one first.</summary>
    public readonly struct VillagerRequestTappedEvent : IGameEvent
    {
        public readonly int Index;
        public VillagerRequestTappedEvent(int index) => Index = index;
    }

    /// <summary>A farm plot that isn't ripe was tapped (own village): the HUD shows its seed picker or status.</summary>
    public readonly struct FarmPlotTappedEvent : IGameEvent
    {
        public readonly int Plot;
        public FarmPlotTappedEvent(int plot) => Plot = plot;
    }

    /// <summary>Placement started, moved to a new validity, or ended — the HUD's placement bar follows it.</summary>
    public readonly struct DecorationPlacementEvent : IGameEvent
    {
        public readonly bool Active;
        public DecorationPlacementEvent(bool active) => Active = active;
    }

    /// <summary>Everything on the village map sorts by where it touches the ground: lower = in front.</summary>
    public static class VillageSorting
    {
        public const int Ground = -1000;
        public const int Paths = -990;
        public const int Labels = 900;

        public static int Order(float feetY) => 500 - Mathf.RoundToInt(feetY * 10f);

        /// <summary>Feet of a centre-pivoted sprite drawn at <paramref name="position"/>.</summary>
        public static float Feet(Vector2 position, Sprite sprite, float scale = 1f) =>
            sprite != null ? position.y - sprite.bounds.extents.y * scale : position.y;
    }
}
