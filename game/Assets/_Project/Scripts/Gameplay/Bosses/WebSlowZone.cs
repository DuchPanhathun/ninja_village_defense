using System.Collections;
using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Gameplay.Bosses
{
    /// <summary>
    /// A temporary trigger zone that slows any player entering it.
    /// Self-destructs after <see cref="duration"/> seconds.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class WebSlowZone : MonoBehaviour
    {
        private float _slowMultiplier;
        private float _duration;
        private LayerMask _playerMask;

        public void Initialize(float radius, float slowMultiplier, float duration, LayerMask playerMask)
        {
            _slowMultiplier = slowMultiplier;
            _duration = duration;
            _playerMask = playerMask;

            var col = GetComponent<CircleCollider2D>();
            col.radius = radius;
            col.isTrigger = true;

            Destroy(gameObject, duration);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & _playerMask) == 0) return;
            if (other.TryGetComponent<StatusEffectReceiver>(out var status))
                status.ApplySlow(_slowMultiplier, _duration);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & _playerMask) == 0) return;
            if (other.TryGetComponent<StatusEffectReceiver>(out var status))
                status.ApplySlow(_slowMultiplier, 0.5f);
        }
    }
}
