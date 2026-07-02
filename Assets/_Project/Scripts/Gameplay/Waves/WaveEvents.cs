using NinjaVillage.Core.Events;

namespace NinjaVillage.Gameplay.Waves
{
    public readonly struct WaveStartedEvent : IGameEvent
    {
        public readonly int WaveNumber;
        public readonly bool IsBossWave;
        public WaveStartedEvent(int waveNumber, bool isBossWave)
        {
            WaveNumber = waveNumber;
            IsBossWave = isBossWave;
        }
    }

    public readonly struct WaveClearedEvent : IGameEvent
    {
        public readonly int WaveNumber;
        public WaveClearedEvent(int waveNumber) => WaveNumber = waveNumber;
    }

    /// <summary>Raised once when every defined wave is cleared and endless mode is off.</summary>
    public readonly struct AllWavesCompleteEvent : IGameEvent { }
}
