using NinjaVillage.Core.Events;

namespace NinjaVillage.Gameplay.Ultimates
{
    /// <summary>Raised whenever ultimate charge changes — drives the ultimate button's fill UI.</summary>
    public readonly struct UltimateChargeChangedEvent : IGameEvent
    {
        /// <summary>0..1. The button lights up at 1.</summary>
        public readonly float Normalized;
        public UltimateChargeChangedEvent(float normalized) => Normalized = normalized;
    }

    public readonly struct UltimateActivatedEvent : IGameEvent
    {
        public readonly UltimateDefinition Ultimate;
        public UltimateActivatedEvent(UltimateDefinition ultimate) => Ultimate = ultimate;
    }
}
