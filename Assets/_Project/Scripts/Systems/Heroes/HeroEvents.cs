using NinjaVillage.Core.Events;

namespace NinjaVillage.Systems.Heroes
{
    /// <summary>Why a hero action failed (or didn't) — the UI turns this into a hint/toast.</summary>
    public enum HeroActionResult
    {
        Success,
        InvalidHero,
        AlreadyUnlocked,
        NotUnlocked,
        NotEnoughCurrency,
        DojoLevelTooLow,
        LevelCapReached,
        MaxLevel
    }

    /// <summary>Raised when a hero is unlocked (bought, granted by the store, or the free starter).</summary>
    public readonly struct HeroUnlockedEvent : IGameEvent
    {
        public readonly string HeroId;
        public HeroUnlockedEvent(string heroId) => HeroId = heroId;
    }

    /// <summary>Raised when the player picks the hero they take into the next run.</summary>
    public readonly struct HeroSelectedEvent : IGameEvent
    {
        public readonly string HeroId;
        public HeroSelectedEvent(string heroId) => HeroId = heroId;
    }

    /// <summary>Raised after a hero is upgraded (EPIC 13 "Hero progression").</summary>
    public readonly struct HeroLevelChangedEvent : IGameEvent
    {
        public readonly string HeroId;
        public readonly int NewLevel;

        public HeroLevelChangedEvent(string heroId, int newLevel)
        {
            HeroId = heroId;
            NewLevel = newLevel;
        }
    }
}
