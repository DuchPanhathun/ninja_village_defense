using System.Collections;
using NinjaVillage.Core.Combat;
using UnityEngine;

namespace NinjaVillage.Gameplay.Combat
{
    /// <summary>
    /// Tints the sprite briefly whenever this entity takes a hit. Put on the same
    /// GameObject as Health (player and enemy prefabs alike).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color flashColor = new(1f, 0.3f, 0.3f);
        [SerializeField] private float flashDuration = 0.08f;

        private Health _health;
        private Color _originalColor;
        private Coroutine _activeFlash;

        private void Awake()
        {
            _health = GetComponent<Health>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                _originalColor = spriteRenderer.color;
        }

        private void OnEnable() => _health.OnDamaged += OnDamaged;
        private void OnDisable()
        {
            _health.OnDamaged -= OnDamaged;
            if (spriteRenderer != null)
                spriteRenderer.color = _originalColor;
        }

        private void OnDamaged(float amount, float current, float max)
        {
            if (spriteRenderer == null) return;
            if (_activeFlash != null) StopCoroutine(_activeFlash);
            _activeFlash = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = _originalColor;
            _activeFlash = null;
        }
    }
}
