using NinjaVillage.Core.Events;

namespace NinjaVillage.Gameplay.Village
{
    /// <summary>Anything in the village world the player can tap (buildings, villagers).</summary>
    public interface IVillageTappable
    {
        void OnTapped();
    }

    /// <summary>Raised when the player taps a building on the village map; the village HUD opens its menu.</summary>
    public readonly struct VillageBuildingTappedEvent : IGameEvent
    {
        public readonly string BuildingId;
        public VillageBuildingTappedEvent(string buildingId) => BuildingId = buildingId;
    }
}
