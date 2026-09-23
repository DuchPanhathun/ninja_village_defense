using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Enemies;

namespace NinjaVillage.Gameplay.Bosses
{
    /// <summary>Raised when a boss enters the arena. Carries its Health so the boss bar UI can track it.</summary>
    public readonly struct BossSpawnedEvent : IGameEvent
    {
        public readonly EnemyDefinition Definition;
        public readonly Health Health;
        public BossSpawnedEvent(EnemyDefinition definition, Health health)
        {
            Definition = definition;
            Health = health;
        }
    }

    /// <summary>Raised each time a boss crosses a phase health threshold.</summary>
    public readonly struct BossPhaseChangedEvent : IGameEvent
    {
        public readonly int NewPhase;
        public BossPhaseChangedEvent(int newPhase) => NewPhase = newPhase;
    }

    /// <summary>Raised once when a boss dies — drives run stats, quests, achievements, music and analytics.</summary>
    public readonly struct BossDefeatedEvent : IGameEvent
    {
        public readonly EnemyDefinition Definition;
        public readonly UnityEngine.Vector2 Position;
        public BossDefeatedEvent(EnemyDefinition definition, UnityEngine.Vector2 position)
        {
            Definition = definition;
            Position = position;
        }
    }
}
