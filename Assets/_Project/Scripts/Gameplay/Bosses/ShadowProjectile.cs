using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Bosses
{
    /// <summary>
    /// Simple straight-flying projectile for the Shadow Ninja — separate from the
    /// player's <c>Projectile</c> so it targets the player layer rather than enemies.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ShadowProjectile : MonoBehaviour
    {
        private float _damage;
        private LayerMask _hitMask;
        private GameObject _source;
        private float _spawnTime;
        private const float Lifetime = 4f;

        public void Launch(Vector2 direction, float speed, float damage, LayerMask hitMask, GameObject source)
        {
            _damage = damage;
            _hitMask = hitMask;
            _source = source;
            _spawnTime = Time.time;

            var rb = GetComponent<Rigidbody2D>();
            rb.linearVelocity = direction.normalized * speed;
            transform.right = direction;
        }

        private void Update()
        {
            if (Time.time - _spawnTime >= Lifetime)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & _hitMask) == 0) return;
            if (!other.TryGetComponent<IDamageable>(out var target) || !target.IsAlive) return;

            Vector2 dir = GetComponent<Rigidbody2D>().linearVelocity.normalized;
            target.TakeDamage(new DamageInfo(_damage, false, dir, 3f, _source));
            Destroy(gameObject);
        }
    }
}
