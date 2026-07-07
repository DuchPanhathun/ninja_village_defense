using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Bosses
{
    /// <summary>
    /// Shadow Ninja mechanics:
    ///  - MIRROR MOVEMENT: moves in the opposite horizontal direction the player moves,
    ///    always closing to mid-range to maintain threat without pure chase.
    ///  - CLONE ATTACKS: auto-fires projectiles at the player on a cooldown, mirroring
    ///    the player's own weapon damage (reads PlayerStats so it scales with the run).
    /// </summary>
    public class ShadowNinjaBoss : BossController
    {
        [Header("Mirror Movement")]
        [Tooltip("How far the boss tries to stay from the player (mirrors closing distance).")]
        [SerializeField] private float preferredRange = 3.5f;

        [Header("Clone Attack")]
        [SerializeField] private GameObject shadowProjectilePrefab;
        [SerializeField] private float attackCooldown = 1.8f;
        [SerializeField] private float projectileSpeed = 11f;
        [Tooltip("Fraction of the player's current attack damage the clone deals.")]
        [SerializeField] private float damageFraction = 0.7f;
        [SerializeField] private LayerMask playerMask;

        private float _attackReadyAt;
        private PlayerStats _playerStats;

        protected override void Awake()
        {
            base.Awake();
            if (PlayerReference.Instance != null)
                _playerStats = PlayerReference.Instance.GetComponent<PlayerStats>();
        }

        protected override void TickBehavior()
        {
            if (PlayerTransform == null) return;

            MirrorMovement();

            if (Time.time >= _attackReadyAt)
                CloneAttack();
        }

        protected override void OnPhaseStarted(int phase)
        {
            attackCooldown *= 0.7f;
            damageFraction += 0.1f;
        }

        private void MirrorMovement()
        {
            float distance = Vector2.Distance(transform.position, PlayerTransform.position);

            if (distance > preferredRange + 0.5f)
            {
                // Close in when too far.
                MoveToward(PlayerTransform.position);
            }
            else if (distance < preferredRange - 0.5f)
            {
                // Back away when too close.
                Vector2 awayDir = ((Vector2)transform.position - (Vector2)PlayerTransform.position).normalized;
                Body.linearVelocity = awayDir * (Definition != null ? Definition.MoveSpeed : 3f);
            }
            else
            {
                Body.linearVelocity = Vector2.zero;
            }
        }

        private void CloneAttack()
        {
            _attackReadyAt = Time.time + attackCooldown;

            if (shadowProjectilePrefab == null || PlayerTransform == null) return;

            float damage = 10f;
            if (_playerStats != null)
            {
                // Mirror the player's scaled damage, reduced by damageFraction.
                damage = 10f * _playerStats.AttackDamageMultiplier * damageFraction;
            }

            Vector2 direction = ((Vector2)PlayerTransform.position - (Vector2)transform.position).normalized;
            var instance = Instantiate(shadowProjectilePrefab, transform.position, Quaternion.identity);
            if (instance.TryGetComponent<ShadowProjectile>(out var proj))
                proj.Launch(direction, projectileSpeed, damage, playerMask, gameObject);
        }
    }

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
