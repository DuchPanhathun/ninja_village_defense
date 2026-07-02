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

    /// <summary>Asks the camera to shake — raised by heavy impacts like boss ground smashes.</summary>
    public readonly struct CameraShakeRequestEvent : IGameEvent
    {
        public readonly float Duration;
        public readonly float Magnitude;
        public CameraShakeRequestEvent(float duration, float magnitude)
        {
            Duration = duration;
            Magnitude = magnitude;
        }
    }

    /// <summary>Raised whenever any entity takes a direct hit — drives damage numbers and hit feedback.</summary>
    public readonly struct EntityDamagedEvent : IGameEvent
    {
        public readonly Vector2 Position;
        public readonly float Amount;
        public readonly bool IsCritical;
        public EntityDamagedEvent(Vector2 position, float amount, bool isCritical)
        {
            Position = position;
            Amount = amount;
            IsCritical = isCritical;
        }
    }
}
