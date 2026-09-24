using System.Collections.Generic;
using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Combat;
using NinjaVillage.Gameplay.Combat;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills.Behaviors
{
    /// <summary>
    /// Thunder Kunai evolution: every interval, lightning strikes the nearest demon and chains to the
    /// next-nearest un-struck demon within <c>chainRadius</c>, up to <c>chains</c> hops, stunning each.
    /// </summary>
    public class ThunderKunaiBehavior : MonoBehaviour
    {
        private const float StartRange = 8f;

        private float _damage, _interval, _chainRadius, _stun;
        private int _chains;
        private float _nextAt;
        private LayerMask _enemyMask;
        private readonly HashSet<Transform> _struck = new();

        public void Configure(float damage, float interval, int chains, float chainRadius, float stunSeconds)
        {
            _damage = damage;
            _interval = interval;
            _chains = Mathf.Max(1, chains);
            _chainRadius = chainRadius;
            _stun = stunSeconds;
            if (TryGetComponent<AutoAttackController>(out var attack)) _enemyMask = attack.EnemyMask;
        }

        private void Update()
        {
            if (Time.time < _nextAt || _interval <= 0f) return;

            var target = TargetFinder.FindNearest(transform.position, StartRange, _enemyMask);
            if (target == null) return;
            _nextAt = Time.time + _interval;

            _struck.Clear();
            for (int hop = 0; hop < _chains && target != null; hop++)
            {
                _struck.Add(target);
                if (target.TryGetComponent<IDamageable>(out var damageable) && damageable.IsAlive)
                {
                    damageable.TakeDamage(new DamageInfo(_damage, false, Vector2.zero, 0f, gameObject));
                    if (target.TryGetComponent<StatusEffectReceiver>(out var status)) status.ApplyStun(_stun);
                }
                NinjaVillage.Gameplay.Vfx.Vfx.Lightning(target.position);
                target = TargetFinder.FindNearestExcluding(target.position, _chainRadius, _enemyMask, null, _struck);
            }
            Sfx.PlayAt(AudioCueIds.LightningStrike, transform.position);
        }
    }
}
