using UnityEngine;

namespace NinjaVillage.Core.Events
{
    // ---------------------------------------------------------------------
    // Core gameplay events. Keep each a readonly struct so raising is
    // allocation-free. Add new events here (or in feature-local files) as
    // systems come online — Player, Combat, Waves, XP, Loot, UI, etc.
    // ---------------------------------------------------------------------

    /// <summary>Raised whenever the player takes damage.</summary>
    public readonly struct PlayerDamagedEvent : IGameEvent
    {
        public readonly float Amount;
        public readonly float RemainingHealth;
        public PlayerDamagedEvent(float amount, float remainingHealth)
        {
            Amount = amount;
            RemainingHealth = remainingHealth;
        }
    }

    /// <summary>Raised once when the player's health reaches zero.</summary>
    public readonly struct PlayerDiedEvent : IGameEvent { }

    /// <summary>Raised when an enemy is killed. Carries where it died for loot/XP spawning.</summary>
    public readonly struct EnemyKilledEvent : IGameEvent
    {
        public readonly Vector2 Position;
        public readonly int XpReward;
        public readonly int CoinReward;
        public EnemyKilledEvent(Vector2 position, int xpReward, int coinReward)
        {
            Position = position;
            XpReward = xpReward;
            CoinReward = coinReward;
        }
    }

    /// <summary>Raised each time the player gains XP (for the XP bar / magnet feedback).</summary>
    public readonly struct XpGainedEvent : IGameEvent
    {
        public readonly int Amount;
        public XpGainedEvent(int amount) => Amount = amount;
    }

    /// <summary>Raised when the player levels up and must pick one of three upgrades.</summary>
    public readonly struct LevelUpEvent : IGameEvent
    {
        public readonly int NewLevel;
        public LevelUpEvent(int newLevel) => NewLevel = newLevel;
    }
}
