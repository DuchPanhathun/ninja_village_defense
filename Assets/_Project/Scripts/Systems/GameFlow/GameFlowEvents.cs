using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.GameFlow
{
    /// <summary>Raised once when the run ends — player died, or all waves cleared (victory).</summary>
    public readonly struct RunEndedEvent : IGameEvent
    {
        public readonly bool Victory;
        public readonly int WaveReached;
        /// <summary>Full stats for this run (kills, coins, duration...). Already recorded into the profile.</summary>
        public readonly RunRecord Summary;

        public RunEndedEvent(bool victory, int waveReached, RunRecord summary = null)
        {
            Victory = victory;
            WaveReached = waveReached;
            Summary = summary;
        }
    }

    /// <summary>Raised when a battle run begins, after the RunBootstrapper has applied all meta-progression bonuses.</summary>
    public readonly struct RunStartedEvent : IGameEvent
    {
        public readonly string HeroId;
        public readonly string WeaponId;
        public RunStartedEvent(string heroId, string weaponId)
        {
            HeroId = heroId;
            WeaponId = weaponId;
        }
    }

    /// <summary>Raised just before <see cref="SceneLoader"/> switches scenes.</summary>
    public readonly struct SceneChangingEvent : IGameEvent
    {
        public readonly string FromScene;
        public readonly string ToScene;
        public SceneChangingEvent(string fromScene, string toScene)
        {
            FromScene = fromScene;
            ToScene = toScene;
        }
    }
}
