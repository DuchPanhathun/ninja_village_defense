using System.Collections;
using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Animation;
using NinjaVillage.Gameplay.Enemies;
using NinjaVillage.Gameplay.World;
using UnityEngine;

namespace NinjaVillage.Gameplay.Bosses
{
    /// <summary>
    /// Base for every boss: an <see cref="EnemyController"/> that announces itself
    /// to the UI and advances through attack phases as its health drops. Concrete
    /// bosses override <see cref="TickBehavior"/> for their signature mechanics and
    /// <see cref="OnPhaseStarted"/> to escalate them. Every boss can also leap: every few seconds when the
    /// player is at mid range (or straight away when a tree or rock has it stuck) it marks the landing spot,
    /// jumps over anything in the way and lands with a damaging shockwave.
    /// </summary>
    public class BossController : EnemyController
    {
        [Tooltip("Health fractions (descending) at which the boss advances a phase, e.g. 0.66, 0.33.")]
        [SerializeField] private float[] phaseHealthThresholds = { 0.66f, 0.33f };

        [Header("Leap")]
        [SerializeField] private bool canLeap = true;
        [SerializeField] private float leapCooldown = 7f;
        [SerializeField] private float leapDuration = 0.85f;
        [SerializeField] private float leapHeight = 3f;
        [SerializeField] private float leapMinDistance = 3f;
        [SerializeField] private float leapMaxDistance = 9f;
        [SerializeField] private float leapRadius = 2.2f;
        [SerializeField] private float leapDamageMultiplier = 1.5f;
        [Tooltip("Seconds of being blocked (barely moving while chasing) before leaping over the obstacle.")]
        [SerializeField] private float stuckSeconds = 0.7f;

        private AirborneVisual _airborne;
        private float _nextLeapAt;
        private float _stuckTime;
        private Vector2 _lastPosition;

        public bool IsLeaping { get; private set; }

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
            _nextLeapAt = Time.time + 2.5f; // let the player see it arrive first
            _lastPosition = transform.position;
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

        protected override void Update()
        {
            if (IsLeaping) return; // the leap drives movement until it lands
            base.Update();
            if (!canLeap || !HealthComponent.IsAlive || PlayerTransform == null || (Status != null && Status.IsStunned)) return;

            // Stuck: wants to chase but has barely moved (a tree or rock in the way).
            Vector2 position = transform.position;
            float distance = Vector2.Distance(position, PlayerTransform.position);
            float moved = (position - _lastPosition).magnitude;
            _lastPosition = position;
            bool chasing = distance > Definition.AttackRange + 0.5f;
            float expected = Definition.MoveSpeed * Time.deltaTime;
            _stuckTime = chasing && moved < expected * 0.3f ? _stuckTime + Time.deltaTime : 0f;

            bool inRange = distance >= leapMinDistance && distance <= leapMaxDistance;
            if (Time.time >= _nextLeapAt && (inRange || _stuckTime >= stuckSeconds))
                StartCoroutine(LeapRoutine());
        }

        private IEnumerator LeapRoutine()
        {
            IsLeaping = true;
            _stuckTime = 0f;
            _nextLeapAt = Time.time + leapCooldown;
            if (_airborne == null && !TryGetComponent(out _airborne)) _airborne = gameObject.AddComponent<AirborneVisual>();

            Vector2 from = transform.position;
            Vector2 to = PlayerTransform != null ? (Vector2)PlayerTransform.position : from;
            if ((to - from).magnitude > leapMaxDistance) to = from + (to - from).normalized * leapMaxDistance;

            // Telegraph: a red circle where it will land.
            var marker = GeneratedSprites.CreateRenderer(null, "LeapTarget", GeneratedSprites.Circle, new Color(1f, 0.15f, 0.1f, 0.3f),
                DepthSort.Band - 300, to, Vector2.one * leapRadius * 2f);

            Body.linearVelocity = Vector2.zero;
            var excluded = Body.excludeLayers;
            Body.excludeLayers = excluded | Obstacles.Mask | (PlayerTransform != null ? 1 << PlayerTransform.gameObject.layer : 0);
            _airborne.Begin();
            if (Mathf.Abs(to.x - from.x) > 0.01f)
            {
                var scale = transform.localScale;
                scale.x = Mathf.Sign(to.x - from.x) * Mathf.Abs(scale.x);
                transform.localScale = scale;
            }

            for (float t = 0f; t < leapDuration; t += Time.deltaTime)
            {
                if (!HealthComponent.IsAlive) break;
                float k = t / leapDuration;
                Body.MovePosition(Vector2.Lerp(from, to, k));
                _airborne.Height = 4f * leapHeight * k * (1f - k);
                marker.color = new Color(1f, 0.15f, 0.1f, 0.25f + 0.3f * k);
                yield return null;
            }

            _airborne.End();
            Body.excludeLayers = excluded;
            Destroy(marker.gameObject);
            _lastPosition = transform.position;
            IsLeaping = false;
            if (!HealthComponent.IsAlive) yield break;

            // Shockwave.
            Vector2 landing = transform.position;
            if (PlayerTransform != null)
                AreaDamage.DamageCircle(landing, leapRadius, 1 << PlayerTransform.gameObject.layer, CurrentDamage * leapDamageMultiplier, 6f, gameObject);
            NinjaVillage.Gameplay.Vfx.Vfx.Burst(landing, new Color(0.85f, 0.75f, 0.55f, 0.9f), leapRadius * 2.2f, 0.4f);
            var shake = FindAnyObjectByType<NinjaVillage.Gameplay.Camera.CameraShake>();
            if (shake != null) shake.Shake(0.25f, 0.35f);
        }

        /// <summary>Hook for concrete bosses to escalate on phase change (faster cooldowns, new attacks...).</summary>
        protected virtual void OnPhaseStarted(int phase) { }
    }
}
