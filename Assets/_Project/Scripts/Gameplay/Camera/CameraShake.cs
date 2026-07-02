using System.Collections;
using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Gameplay.Camera
{
    /// <summary>
    /// Trauma-style camera shake. Auto-shakes a little on <see cref="PlayerDamagedEvent"/>
    /// (decoupled via the EventBus — no direct reference to the player needed) and
    /// exposes <see cref="Shake"/> for scripted moments like boss ground-smashes.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private float damageShakeDuration = 0.15f;
        [SerializeField] private float damageShakeMagnitude = 0.1f;

        private Vector3 _originalLocalPos;
        private Coroutine _activeShake;

        private void Awake()
        {
            _originalLocalPos = transform.localPosition;
        }

        private void OnEnable()
        {
            EventBus<PlayerDamagedEvent>.Subscribe(OnPlayerDamaged);
        }

        private void OnDisable()
        {
            EventBus<PlayerDamagedEvent>.Unsubscribe(OnPlayerDamaged);
        }

        private void OnPlayerDamaged(PlayerDamagedEvent evt)
        {
            Shake(damageShakeDuration, damageShakeMagnitude);
        }

        public void Shake(float duration, float magnitude)
        {
            if (_activeShake != null) StopCoroutine(_activeShake);
            _activeShake = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                Vector2 offset = Random.insideUnitCircle * magnitude;
                transform.localPosition = _originalLocalPos + (Vector3)offset;
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localPosition = _originalLocalPos;
            _activeShake = null;
        }
    }
}
