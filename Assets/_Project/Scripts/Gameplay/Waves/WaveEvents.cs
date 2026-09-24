using NinjaVillage.Core.Events;

namespace NinjaVillage.Gameplay.Waves
{
    public readonly struct WaveStartedEvent : IGameEvent
    {
        public readonly int WaveNumber;
        public readonly bool IsBossWave;
        /// <summary>Waves in the run's plan; 0 when endless.</summary>
        public readonly int TotalWaves;
        /// <summary>The boss this wave ends with (null when none).</summary>
        public readonly string BossName;

        public WaveStartedEvent(int waveNumber, bool isBossWave, int totalWaves = 0, string bossName = null)
        {
            WaveNumber = waveNumber;
            IsBossWave = isBossWave;
            TotalWaves = totalWaves;
            BossName = bossName;
        }

        public bool IsFinalWave => TotalWaves > 0 && WaveNumber >= TotalWaves;
    }

    public readonly struct WaveClearedEvent : IGameEvent
    {
        public readonly int WaveNumber;
        public WaveClearedEvent(int waveNumber) => WaveNumber = waveNumber;
    }

    /// <summary>Raised once when every defined wave is cleared and endless mode is off.</summary>
    public readonly struct AllWavesCompleteEvent : IGameEvent { }
}
