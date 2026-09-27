using System.Collections;
using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Gameplay.Bosses
{
    /// <summary>
    /// A clone that rushes the player and explodes on contact.
    /// </summary>
    public class IllusionClone : MonoBehaviour
    {
        private Transform _target;
        private float _moveSpeed;
        private float _explosionRadius;
        private float _explosionDamage;
        private LayerMask _playerMask;
        private GameObject _source;
        private bool _exploded;
        private const float Lifetime = 8f;

        public void Initialize(Transform target, float moveSpeed, float explosionRadius, float explosionDamage, LayerMask playerMask, GameObject source)
        {
            _target = target;
            _moveSpeed = moveSpeed;
            _explosionRadius = explosionRadius;
            _explosionDamage = explosionDamage;
            _playerMask = playerMask;
            _source = source;
            Destroy(gameObject, Lifetime);
        }

        private void Update()
        {
            if (_exploded || _target == null) return;

            Vector2 dir = ((Vector2)_target.position - (Vector2)transform.position).normalized;
            transform.position += (Vector3)(dir * _moveSpeed * Time.deltaTime);

            if (Vector2.Distance(transform.position, _target.position) < _explosionRadius * 0.5f)
                Explode();
        }

        private void Explode()
        {
            if (_exploded) return;
            _exploded = true;

            AreaDamage.DamageCircle(transform.position, _explosionRadius, _playerMask, _explosionDamage, 5f, _source);
            EventBus<CameraShakeRequestEvent>.Raise(new CameraShakeRequestEvent(0.2f, 0.15f));
            Destroy(gameObject);
        }
    }
}
