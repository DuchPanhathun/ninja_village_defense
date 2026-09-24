using System.Collections;
using NinjaVillage.Core.Combat;
using UnityEngine;

namespace NinjaVillage.Gameplay.Animation
{
    /// <summary>
    /// Code-driven placeholder animation for any sprite character (EPIC 1 "Movement animation",
    /// EPIC 3 "Enemy animation") until real sprite clips exist: a hopping squash-and-stretch while
    /// walking, gentle breathing while idle, a squash when hit, a lunge punch when attacking, and a
    /// shrink-and-fade death.
    ///
    /// It only animates scale magnitude (never position, which the Rigidbody owns) and preserves the
    /// sign of localScale.x, which the Player/Enemy controllers flip to face their movement. Added
    /// automatically by those controllers when no Animator Controller is assigned.
    /// </summary>
    [DisallowMultipleComponent]
    public class ProceduralSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private float walkFrequency = 14f;
        [SerializeField] private float walkStretch = 0.10f;
        [SerializeField] private float idleBreath = 0.03f;
        [SerializeField] private float hitSquash = 0.25f;
        [SerializeField] private float punchStretch = 0.2f;
        [SerializeField] private float movingSpeedThreshold = 0.2f;

        private Vector2 _baseScale;
        private bool _hasBase;
        private Vector3 _lastPosition;
        private float _phase;
        private float _squash;   // 1 → 0 after a hit
        private float _punch;    // 1 → 0 after an attack
        private bool _dying;
        private Health _health;
        private SpriteRenderer _renderer;

        private Color _rendererColor = Color.white;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null) _rendererColor = _renderer.color;
            _phase = Random.value * 10f; // desync crowds
        }

        private void OnEnable()
        {
            if (_health == null) return;
            _health.OnDamaged += OnDamaged;
            _health.OnDeath += OnDeath;
        }

        private void OnDisable()
        {
            if (_health == null) return;
            _health.OnDamaged -= OnDamaged;
            _health.OnDeath -= OnDeath;
        }

        private void Start()
        {
            // After Initialize/spawn setup, so any size set by the spawner is the base.
            var s = transform.localScale;
            _baseScale = new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.y));
            _hasBase = true;
            _lastPosition = transform.position;
        }

        /// <summary>Undoes the death shrink/fade when the character is revived.</summary>
        public void ResetAfterRevive()
        {
            StopAllCoroutines();
            _dying = false;
            if (_renderer != null) _renderer.color = _rendererColor;
            if (_hasBase)
            {
                float sign = transform.localScale.x < 0f ? -1f : 1f;
                transform.localScale = new Vector3(sign * _baseScale.x, _baseScale.y, transform.localScale.z);
            }
        }

        /// <summary>A quick forward stretch — call when this character attacks.</summary>
        public void Punch(float strength = 1f) => _punch = Mathf.Max(_punch, Mathf.Clamp01(strength));

        private void OnDamaged(float amount, float current, float max) => _squash = 1f;

        private void OnDeath(Health health)
        {
            if (_dying || !isActiveAndEnabled) return;
            _dying = true;
            if (_renderer != null) _rendererColor = _renderer.color; // current tint (e.g. a skin), restored on revive
            StartCoroutine(DeathRoutine());
        }

        private void LateUpdate()
        {
            if (!_hasBase || _dying) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return; // paused

            Vector3 position = transform.position;
            float speed = (position - _lastPosition).magnitude / dt;
            _lastPosition = position;
            bool moving = speed > movingSpeedThreshold;

            _phase += dt * (moving ? walkFrequency : 2.5f);
            float stretch = moving ? Mathf.Abs(Mathf.Sin(_phase)) * walkStretch : Mathf.Sin(_phase) * idleBreath;

            float sy = 1f + stretch;
            float sx = 1f - stretch * 0.5f; // roughly volume-preserving

            if (_squash > 0f)
            {
                sx *= 1f + hitSquash * _squash;
                sy *= 1f - hitSquash * _squash;
                _squash = Mathf.Max(0f, _squash - dt * 8f);
            }
            if (_punch > 0f)
            {
                sx *= 1f + punchStretch * _punch;
                sy *= 1f - punchStretch * 0.5f * _punch;
                _punch = Mathf.Max(0f, _punch - dt * 7f);
            }

            float sign = transform.localScale.x < 0f ? -1f : 1f; // keep the controller's facing flip
            transform.localScale = new Vector3(sign * _baseScale.x * sx, _baseScale.y * sy, transform.localScale.z);
        }

        private IEnumerator DeathRoutine()
        {
            Vector3 start = transform.localScale;
            Color startColor = _renderer != null ? _renderer.color : Color.white;
            const float duration = 0.6f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                transform.localScale = new Vector3(start.x * (1f + k * 0.3f), start.y * (1f - k), start.z);
                if (_renderer != null)
                {
                    var c = startColor;
                    c.a = startColor.a * (1f - k);
                    _renderer.color = c;
                }
                yield return null;
            }
        }
    }
}
