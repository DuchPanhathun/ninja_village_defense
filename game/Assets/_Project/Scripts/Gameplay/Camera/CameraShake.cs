using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Gameplay.Camera
{
    /// <summary>
    /// Camera shake for big moments (boss slams and landings, Dragon Slash) via <see cref="Shake"/> or
    /// <see cref="CameraShakeRequestEvent"/>. The shake is an offset added on top of wherever the camera
    /// already is: it's taken off before anything else moves the camera each frame and put back after the
    /// follow (<see cref="CameraFollow"/>) has run, so the view always stays on the player. Taking a hit
    /// doesn't shake the screen (the hit flash and vibration say it) — constant shaking in a crowd made the
    /// player hard to steer. <see cref="Enabled"/> is the player's "Screen shake" setting.
    /// </summary>
    [DefaultExecutionOrder(500)] // LateUpdate after CameraFollow
    public class CameraShake : MonoBehaviour
    {
        /// <summary>Settings → Screen shake. Off = no shakes at all.</summary>
        public static bool Enabled { get; set; } = true;

        private float _duration, _magnitude, _elapsed;
        private Vector3 _applied;

        private void OnEnable() => EventBus<CameraShakeRequestEvent>.Subscribe(OnShakeRequested);

        private void OnDisable()
        {
            EventBus<CameraShakeRequestEvent>.Unsubscribe(OnShakeRequested);
            RemoveOffset();
        }

        private void OnShakeRequested(CameraShakeRequestEvent evt) => Shake(evt.Duration, evt.Magnitude);

        /// <summary>Shakes for <paramref name="duration"/> seconds; a stronger shake overrides a weaker one.</summary>
        public void Shake(float duration, float magnitude)
        {
            if (!Enabled || duration <= 0f || magnitude <= 0f) return;
            bool active = _elapsed < _duration;
            if (active && magnitude < _magnitude * (1f - _elapsed / _duration)) return;
            _duration = duration;
            _magnitude = magnitude;
            _elapsed = 0f;
        }

        // Before any other script reads or moves the camera this frame.
        private void Update() => RemoveOffset();

        private void LateUpdate()
        {
            if (!Enabled || _elapsed >= _duration) return;
            _elapsed += Time.deltaTime;
            float strength = _magnitude * (1f - Mathf.Clamp01(_elapsed / _duration)); // eases out
            _applied = (Vector3)(Random.insideUnitCircle * strength);
            transform.position += _applied;
        }

        private void RemoveOffset()
        {
            if (_applied == Vector3.zero) return;
            transform.position -= _applied;
            _applied = Vector3.zero;
        }
    }
}
