using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NinjaVillage.Gameplay.Player
{
    /// <summary>
    /// Dash (EPIC 1 "Add dash movement support"): a short burst in the movement direction with
    /// invulnerability frames, on a cooldown — the one active defensive move in an otherwise
    /// movement-only control scheme. Triggered by Space (Editor/desktop) or the on-screen dash button
    /// (<see cref="NinjaVillage.UI.Battle.DashButtonUI"/>, created automatically).
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(Health))]
    public class DashController : MonoBehaviour
    {
        [SerializeField] private float distance = 3.5f;
        [SerializeField] private float duration = 0.18f;
        [SerializeField] private float cooldown = 2.5f;
        [SerializeField] private float invulnerabilitySeconds = 0.35f;

        private PlayerController _controller;
        private Health _health;
        private float _readyAt;

        public float Cooldown => cooldown;
        /// <summary>0 = ready, 1 = just used.</summary>
        public float CooldownNormalized => cooldown <= 0f ? 0f : Mathf.Clamp01((_readyAt - Time.time) / cooldown);
        public bool IsReady => Time.time >= _readyAt;

        /// <summary>Raised when the Dash button UI should be built (kept in UI code — see DashButtonUI).</summary>
        public static event System.Action<DashController> Spawned;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _health = GetComponent<Health>();
        }

        private void Start() => Spawned?.Invoke(this);

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) TryDash();
        }

        public bool TryDash()
        {
            if (!IsReady || _controller.IsDead || Time.timeScale <= 0f) return false;

            Vector2 direction = _controller.FacingDirection;
            if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;

            _controller.ApplyDash(direction.normalized * (distance / duration), duration);
            _health.GrantInvulnerability(invulnerabilitySeconds);
            _readyAt = Time.time + cooldown;

            Sfx.PlayAt(AudioCueIds.Dash, transform.position);
            // After-image where the dash started.
            NinjaVillage.Gameplay.Vfx.Vfx.Spawn(GeneratedSprites.Glow, transform.position, new Color(0.6f, 0.8f, 1f, 0.7f), 1.2f, 0.4f, 0.25f);
            return true;
        }
    }
}
