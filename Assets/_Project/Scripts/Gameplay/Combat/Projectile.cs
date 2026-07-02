using NinjaVillage.Core.Combat;
using UnityEngine;

namespace NinjaVillage.Gameplay.Combat
{
    /// <summary>
    /// Generic auto-attack projectile: flies straight, damages the first (or up to
    /// <see cref="pierceCount"/>) damageables on <see cref="hitMask"/>, then despawns.
    /// Used by every basic ranged weapon (Kunai now, Shuriken/Bow later reuse it too).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private int pierceCount = 0;

        private Rigidbody2D _rigidbody;
        private Vector2 _velocity;
        private float _damage;
        private bool _isCritical;
        private float _knockbackForce;
        private LayerMask _hitMask;
        private GameObject _source;
        private int _remainingPierces;
        private float _spawnTime;
        private StatusPayload _statusPayload;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        /// <summary>Configures a freshly spawned/pooled projectile. Call immediately after Instantiate.</summary>
        public void Launch(Vector2 direction, float speed, float damage, bool isCritical, float knockbackForce, LayerMask hitMask, GameObject source)
        {
            _velocity = direction.normalized * speed;
            _damage = damage;
            _isCritical = isCritical;
            _knockbackForce = knockbackForce;
            _hitMask = hitMask;
            _source = source;
            _remainingPierces = pierceCount;
            _spawnTime = Time.time;

            transform.right = direction;
            _rigidbody.linearVelocity = _velocity;
            _statusPayload = default;
        }

        /// <summary>Optional on-hit status effects (burn/poison). Call after <see cref="Launch"/>.</summary>
        public void SetStatusPayload(in StatusPayload payload) => _statusPayload = payload;

        private void Update()
        {
            if (Time.time - _spawnTime >= lifetime)
                Despawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & _hitMask) == 0) return;
            if (!other.TryGetComponent<IDamageable>(out var damageable) || !damageable.IsAlive) return;

            var damage = new DamageInfo(_damage, _isCritical, _velocity, _knockbackForce, _source);
            damageable.TakeDamage(damage);

            if (_statusPayload.HasAny && other.TryGetComponent<StatusEffectReceiver>(out var status))
                status.Apply(_statusPayload);

            if (_remainingPierces <= 0)
            {
                Despawn();
            }
            else
            {
                _remainingPierces--;
            }
        }

        private void Despawn()
        {
            // TODO(pooling): route through an object pool once perf work starts (EPIC 23).
            Destroy(gameObject);
        }
    }
}
