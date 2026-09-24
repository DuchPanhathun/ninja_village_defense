using System.Collections;
using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Animation;
using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Gameplay.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NinjaVillage.Gameplay.Player
{
    /// <summary>
    /// The player's jump: a short hop in an arc (the sprite rises over a ground shadow) during which the
    /// player passes over trees, rocks and enemies and can't be hit — keep steering to hop between
    /// obstacles or out of a crowd. Landing inside something solid keeps you airborne a moment longer until
    /// you're clear. On-screen button (<see cref="Spawned"/> → JumpButtonUI) or J in the Editor.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(Health))]
    public class JumpController : MonoBehaviour
    {
        [SerializeField] private float duration = 0.6f;
        [SerializeField] private float height = 1.5f;
        [SerializeField] private float cooldown = 1.8f;
        [Tooltip("Extra time allowed to clear an obstacle the jump would otherwise land in.")]
        [SerializeField] private float maxHangTime = 0.5f;

        private PlayerController _controller;
        private Health _health;
        private Rigidbody2D _body;
        private AirborneVisual _airborne;
        private float _readyAt;
        private LayerMask _passOver;
        private LayerMask _excludedBefore;

        public float Cooldown => cooldown;
        public float CooldownNormalized => cooldown <= 0f ? 0f : Mathf.Clamp01((_readyAt - Time.time) / cooldown);
        public bool IsReady => Time.time >= _readyAt && !IsJumping;
        public bool IsJumping { get; private set; }

        /// <summary>Raised when the Jump button UI should be built (kept in UI code — see JumpButtonUI).</summary>
        public static event System.Action<JumpController> Spawned;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _health = GetComponent<Health>();
            _body = GetComponent<Rigidbody2D>();
            _airborne = GetComponent<AirborneVisual>();
            if (_airborne == null) _airborne = gameObject.AddComponent<AirborneVisual>();
        }

        private void Start()
        {
            var attack = GetComponent<AutoAttackController>();
            _passOver = Obstacles.Mask | (attack != null ? attack.EnemyMask : (LayerMask)0);
            Spawned?.Invoke(this);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.jKey.wasPressedThisFrame) TryJump();
        }

        public bool TryJump()
        {
            if (!IsReady || _controller.IsDead || Time.timeScale <= 0f) return false;
            StartCoroutine(JumpRoutine());
            return true;
        }

        private IEnumerator JumpRoutine()
        {
            IsJumping = true;
            _readyAt = Time.time + cooldown;
            _excludedBefore = _body.excludeLayers;
            _body.excludeLayers = _excludedBefore | _passOver;
            _health.GrantInvulnerability(duration + maxHangTime);
            _airborne.Begin();
            Sfx.PlayAt(AudioCueIds.Dash, transform.position);

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                _airborne.Height = 4f * height * k * (1f - k);
                yield return null;
            }

            // Don't land inside a tree: hang low until clear (or give up and let physics push us out).
            for (float hang = 0f; hang < maxHangTime && Physics2D.OverlapCircle(_body.position, 0.35f, Obstacles.Mask) != null; hang += Time.deltaTime)
            {
                _airborne.Height = 0.25f;
                yield return null;
            }

            Land();
            // A little dust where we land.
            NinjaVillage.Gameplay.Vfx.Vfx.Spawn(GeneratedSprites.Glow, transform.position + Vector3.down * 0.4f, new Color(0.9f, 0.85f, 0.7f, 0.6f), 1.1f, 0.3f, 0.2f);
        }

        private void Land()
        {
            _airborne.End();
            _body.excludeLayers = _excludedBefore;
            IsJumping = false;
        }

        private void OnDisable()
        {
            if (IsJumping) Land();
        }
    }
}
