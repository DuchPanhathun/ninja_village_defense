using NinjaVillage.Core.Combat;
using NinjaVillage.Gameplay.Combat;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills.Behaviors
{
    /// <summary>
    /// Granted by the Explosive Bomb skill: periodically detonates an AoE blast at
    /// a random point near the player, damaging every enemy in the blast radius.
    /// </summary>
    public class ExplosiveBombBehavior : MonoBehaviour
    {
        private float _damage;
        private float _interval;
        private float _blastRadius;
        private float _throwRadius;
        private float _nextBombAt;
        private LayerMask _enemyMask;
        private bool _maskResolved;

        public void Configure(float damage, float interval, float blastRadius, float throwRadius)
        {
            _damage = damage;
            _interval = interval;
            _blastRadius = blastRadius;
            _throwRadius = throwRadius;
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
            if (!_maskResolved || Time.time < _nextBombAt) return;

            Vector2 blastCenter = (Vector2)transform.position + Random.insideUnitCircle * _throwRadius;
            int hits = AreaDamage.DamageCircle(blastCenter, _blastRadius, _enemyMask, _damage, 4f, gameObject);

            // Only consume the cooldown when the bomb actually lands near something,
            // so the skill doesn't feel wasted while kiting in open space.
            if (hits > 0)
                _nextBombAt = Time.time + _interval;

            // TODO(VFX): explosion effect at blastCenter (EPIC 23).
        }
    }
}
