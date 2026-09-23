using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Input;
using UnityEngine;

namespace NinjaVillage.Gameplay.Player
{
    /// <summary>
    /// Player movement + life-cycle. Per the design vision, the player ONLY controls
    /// movement — attacking is fully automatic (see <c>AutoAttackController</c>).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Any component implementing IMoveInputProvider — VirtualJoystick on mobile, KeyboardMoveInputProvider in-Editor.")]
        [SerializeField] private MonoBehaviour moveInputSource;

        [Header("Animation (optional)")]
        [SerializeField] private Animator animator;
        [SerializeField] private bool flipSpriteWithDirection = true;

        private IMoveInputProvider _moveInput;
        private Rigidbody2D _rigidbody;
        private Health _health;
        private PlayerStats _stats;
        private Vector2 _currentMoveDir;
        private bool _isDead;
        private float _healAccumulator;

        private static readonly int SpeedParam = Animator.StringToHash("Speed");

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
            _stats = GetComponent<PlayerStats>();
            _moveInput = moveInputSource as IMoveInputProvider;
            _health.DodgeRoll = () => Random.value < _stats.DodgeChance;
            _health.IncomingDamageModifier = amount => amount * (1f - _stats.DamageReduction);

            if (_moveInput == null)
                Debug.LogWarning($"{nameof(PlayerController)} on {name}: moveInputSource does not implement IMoveInputProvider.", this);
        }

        private void OnEnable()
        {
            _health.OnDamaged += HandleDamaged;
            _health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            _health.OnDamaged -= HandleDamaged;
            _health.OnDeath -= HandleDeath;
        }

        private void Update()
        {
            if (_isDead) return;

            TickAutoHeal();

            if (_moveInput == null) return;

            _currentMoveDir = Vector2.ClampMagnitude(_moveInput.GetMoveInput(), 1f);

            if (animator != null)
                animator.SetFloat(SpeedParam, _currentMoveDir.sqrMagnitude);

            if (flipSpriteWithDirection && Mathf.Abs(_currentMoveDir.x) > 0.01f)
            {
                var scale = transform.localScale;
                scale.x = Mathf.Sign(_currentMoveDir.x) * Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }

        private void FixedUpdate()
        {
            if (_isDead) return;
            _rigidbody.MovePosition(_rigidbody.position + _currentMoveDir * (_stats.MoveSpeed * Time.fixedDeltaTime));
        }

        private void HandleDamaged(float amount, float current, float max)
        {
            EventBus<PlayerDamagedEvent>.Raise(new PlayerDamagedEvent(amount, current));
        }

        private void HandleDeath(Health health)
        {
            _isDead = true;
            _currentMoveDir = Vector2.zero;
            if (animator != null)
                animator.SetTrigger("Die");

            EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent());
        }

        /// <summary>Call when starting/restarting a run so a previous death doesn't carry over.</summary>
        public void ResetForNewRun()
        {
            _isDead = false;
            _health.ResetHealth();
        }

        private void TickAutoHeal()
        {
            if (_stats.HealPerSecond <= 0f) return;

            _healAccumulator += _stats.HealPerSecond * Time.deltaTime;
            if (_healAccumulator < 1f) return;

            int wholeHp = Mathf.FloorToInt(_healAccumulator);
            _health.Heal(wholeHp);
            _healAccumulator -= wholeHp;
        }
    }
}
