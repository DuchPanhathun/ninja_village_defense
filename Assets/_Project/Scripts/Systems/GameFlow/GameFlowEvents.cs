using NinjaVillage.Core.Events;

namespace NinjaVillage.Systems.GameFlow
{
    /// <summary>Raised once when the run ends — player died, or all waves cleared (victory).</summary>
    public readonly struct RunEndedEvent : IGameEvent
    {
        public readonly bool Victory;
        public readonly int WaveReached;
        public RunEndedEvent(bool victory, int waveReached)
        {
            Victory = victory;
            WaveReached = waveReached;
        }
    }
}
