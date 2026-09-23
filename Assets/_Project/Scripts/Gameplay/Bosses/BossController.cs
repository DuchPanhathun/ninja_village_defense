using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Enemies;
using UnityEngine;

namespace NinjaVillage.Gameplay.Bosses
{
    /// <summary>
    /// Base for every boss: an <see cref="EnemyController"/> that announces itself
    /// to the UI and advances through attack phases as its health drops. Concrete
    /// bosses override <see cref="TickBehavior"/> for their signature mechanics and
    /// <see cref="OnPhaseStarted"/> to escalate them.
    /// </summary>
    public class BossController : EnemyController
    {
        [Tooltip("Health fractions (descending) at which the boss advances a phase, e.g. 0.66, 0.33.")]
        [SerializeField] private float[] phaseHealthThresholds = { 0.66f, 0.33f };

        /// <summary>1-based. Phase 1 is the opening phase.</summary>
        public int CurrentPhase { get; private set; } = 1;

        private EnemyDefinition _bossDefinition;

        protected override void OnEnable()
        {
            base.OnEnable();
            HealthComponent.OnDamaged += CheckPhaseTransition;
            HealthComponent.OnDeath += AnnounceDefeat;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            HealthComponent.OnDamaged -= CheckPhaseTransition;
            HealthComponent.OnDeath -= AnnounceDefeat;
        }

        public override void Initialize(EnemyDefinition enemyDefinition, float difficultyMultiplier = 1f, bool forceElite = false)
        {
            base.Initialize(enemyDefinition, difficultyMultiplier, forceElite);
            CurrentPhase = 1;
            _bossDefinition = enemyDefinition;
            EventBus<BossSpawnedEvent>.Raise(new BossSpawnedEvent(enemyDefinition, HealthComponent));
        }

        private void CheckPhaseTransition(float amount, float current, float max)
        {
            // CurrentPhase-1 indexes the next threshold to cross.
            while (CurrentPhase - 1 < phaseHealthThresholds.Length &&
                   current / max <= phaseHealthThresholds[CurrentPhase - 1])
            {
                CurrentPhase++;
                EventBus<BossPhaseChangedEvent>.Raise(new BossPhaseChangedEvent(CurrentPhase));
                OnPhaseStarted(CurrentPhase);
            }
        }

        private void AnnounceDefeat(Health health)
        {
            EventBus<BossDefeatedEvent>.Raise(new BossDefeatedEvent(_bossDefinition, transform.position));
        }

        /// <summary>Hook for concrete bosses to escalate on phase change (faster cooldowns, new attacks...).</summary>
        protected virtual void OnPhaseStarted(int phase) { }
    }
}
