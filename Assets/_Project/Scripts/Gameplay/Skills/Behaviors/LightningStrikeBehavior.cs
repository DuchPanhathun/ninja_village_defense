using NinjaVillage.Core.Combat;
using NinjaVillage.Gameplay.Combat;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills.Behaviors
{
    /// <summary>
    /// Granted by the Lightning Strike skill: periodically zaps a random enemy
    /// near the player. Added to the player at runtime by the skill's ApplyLevel,
    /// so it never needs scene wiring.
    /// </summary>
    public class LightningStrikeBehavior : MonoBehaviour
    {
        private static readonly Collider2D[] Buffer = new Collider2D[32];

        private float _damage;
        private float _interval;
        private float _radius;
        private float _nextStrikeAt;
        private LayerMask _enemyMask;
        private bool _maskResolved;

        public void Configure(float damage, float interval, float radius)
        {
            _damage = damage;
            _interval = interval;
            _radius = radius;
        }

        private void Start()
        {
            var autoAttack = GetComponent<AutoAttackController>();
            if (autoAttack != null)
            {
                _enemyMask = autoAttack.EnemyMask;
                _maskResolved = true;
            }
        }

        private void Update()
        {
            if (!_maskResolved || Time.time < _nextStrikeAt) return;

            int count = Physics2D.OverlapCircleNonAlloc(transform.position, _radius, Buffer, _enemyMask);
            if (count == 0) return;

            var target = Buffer[Random.Range(0, count)];
            if (target == null || !target.TryGetComponent<IDamageable>(out var damageable) || !damageable.IsAlive) return;

            damageable.TakeDamage(new DamageInfo(_damage, false, Vector2.zero, 0f, gameObject));
            _nextStrikeAt = Time.time + _interval;

            // TODO(VFX): lightning bolt effect at target.transform.position (EPIC 23).
        }
    }
}
