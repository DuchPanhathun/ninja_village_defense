using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Animation;
using NinjaVillage.Gameplay.World;
using UnityEngine;

namespace NinjaVillage.Gameplay.Enemies
{
    /// <summary>
    /// Shared behavior for every basic enemy: chase the player, melee-attack when
    /// in range, die and report rewards. Enemies with a unique mechanic (bosses)
    /// extend this and override <see cref="TickBehavior"/> and/or add their own
    /// attack scripts alongside it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private Animator animator;

        protected Rigidbody2D Body;
        protected Health HealthComponent;
        protected StatusEffectReceiver Status; // optional
        protected Transform PlayerTransform;

        private float _currentDamage;
        private float _attackCooldownRemaining;
        private bool _isElite;
        private float _probeRadius = 0.45f;
        private float _nextSteerCheck;
        private bool _steering;
        private Vector2 _steerDirection;
        private int _steerSide;
        private float _lastBlockedAt = -10f;

        /// <summary>This enemy's attack damage after difficulty and elite scaling.</summary>
        protected float CurrentDamage => _currentDamage;
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int AttackTrigger = Animator.StringToHash("Attack");
        private static readonly int DieTrigger = Animator.StringToHash("Die");

        public EnemyDefinition Definition => definition;

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            HealthComponent = GetComponent<Health>();
            Status = GetComponent<StatusEffectReceiver>();

            if (!TryGetComponent<DepthSort>(out _)) gameObject.AddComponent<DepthSort>();
            if (TryGetComponent<Collider2D>(out var body))
                _probeRadius = Mathf.Clamp(Mathf.Min(body.bounds.extents.x, body.bounds.extents.y) * 0.9f, 0.2f, 1.2f);

            // No sprite clips yet → code-driven placeholder animation (EPIC 3 "Enemy animation").
            if ((animator == null || animator.runtimeAnimatorController == null) && !TryGetComponent<ProceduralSpriteAnimator>(out _))
                _procedural = gameObject.AddComponent<ProceduralSpriteAnimator>();
            else
                TryGetComponent(out _procedural);
        }

        private ProceduralSpriteAnimator _procedural;

        protected virtual void OnEnable()
        {
            HealthComponent.OnDeath += HandleDeath;
        }

        protected virtual void OnDisable()
        {
            HealthComponent.OnDeath -= HandleDeath;
        }

        /// <summary>Called by the spawner right after Instantiate to configure stats and scaling.</summary>
        public virtual void Initialize(EnemyDefinition enemyDefinition, float difficultyMultiplier = 1f, bool forceElite = false)
        {
            definition = enemyDefinition;
            _isElite = forceElite || enemyDefinition.IsElite;

            float eliteMultiplier = _isElite ? 2.5f : 1f;
            HealthComponent.ResetHealth(enemyDefinition.MaxHealth * difficultyMultiplier * eliteMultiplier);
            _currentDamage = enemyDefinition.Damage * difficultyMultiplier * (_isElite ? 1.5f : 1f);

            PlayerTransform = PlayerReference.Instance != null ? PlayerReference.Instance.PlayerTransform : null;
            MinimapMarker.Add(gameObject, enemyDefinition.IsBoss ? MinimapMarkerKind.Boss
                : _isElite ? MinimapMarkerKind.Elite : MinimapMarkerKind.Enemy);
        }

        protected virtual void Update()
        {
            if (!HealthComponent.IsAlive || PlayerTransform == null) return;

            _attackCooldownRemaining -= Time.deltaTime;
            TickBehavior();
        }

        /// <summary>Default chase-and-melee behavior. Override for bosses with unique mechanics.</summary>
        protected virtual void TickBehavior()
        {
            if (Status != null && Status.IsStunned)
            {
                Body.linearVelocity = Vector2.zero;
                return;
            }

            float distance = Vector2.Distance(transform.position, PlayerTransform.position);

            if (distance > definition.AttackRange)
            {
                MoveToward(PlayerTransform.position);
            }
            else
            {
                Body.linearVelocity = Vector2.zero;
                if (_attackCooldownRemaining <= 0f)
                    Attack();
            }

            if (animator != null)
                animator.SetFloat(SpeedParam, Body.linearVelocity.sqrMagnitude);
        }

        protected void MoveToward(Vector2 worldPosition)
        {
            Vector2 direction = (worldPosition - (Vector2)transform.position).normalized;

            // Trees and rocks: slide around them instead of pushing into them (re-checked a few times a second).
            if (Time.time >= _nextSteerCheck)
            {
                _nextSteerCheck = Time.time + 0.12f;
                var steered = ObstacleAvoidance.Steer(transform.position, direction, _probeRadius, 1.1f, ref _steerSide);
                _steering = steered != direction;
                _steerDirection = steered;
                if (_steering) _lastBlockedAt = Time.time;
                else if (Time.time - _lastBlockedAt > 1.2f) _steerSide = 0; // clear for a while: free to choose again
            }
            if (_steering) direction = _steerDirection;

            float speedMultiplier = Status != null ? Status.MoveSpeedMultiplier : 1f;
            Body.linearVelocity = direction * (definition.MoveSpeed * speedMultiplier);

            if (direction.x != 0f)
            {
                var scale = transform.localScale;
                scale.x = Mathf.Sign(direction.x) * Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }

        protected virtual void Attack()
        {
            _attackCooldownRemaining = definition.AttackCooldown;
            // Explicit null check: ?. bypasses Unity's destroyed/unassigned-object check.
            if (animator != null) animator.SetTrigger(AttackTrigger);
            if (_procedural != null) _procedural.Punch();

            if (PlayerTransform.TryGetComponent<IDamageable>(out var damageable) && damageable.IsAlive)
            {
                Vector2 knockbackDir = (PlayerTransform.position - transform.position).normalized;
                damageable.TakeDamage(new DamageInfo(_currentDamage, false, knockbackDir, 3f, gameObject));
            }
        }

        private void HandleDeath(Health health)
        {
            Body.linearVelocity = Vector2.zero;
            if (animator != null) animator.SetTrigger(DieTrigger);

            EventBus<EnemyKilledEvent>.Raise(new EnemyKilledEvent(transform.position, definition.XpReward, definition.CoinReward));

            // TODO(pooling): despawn via pool once perf work starts (EPIC 23). For now, destroy after a short delay
            // so death animation/VFX has time to play.
            Destroy(gameObject, 1.5f);
            enabled = false;
            if (TryGetComponent<MinimapMarker>(out var marker)) marker.enabled = false; // off the map while the body fades
        }
    }
}
