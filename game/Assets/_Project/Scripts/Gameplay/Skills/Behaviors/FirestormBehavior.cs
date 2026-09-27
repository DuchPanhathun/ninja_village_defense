using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Combat;
using NinjaVillage.Gameplay.Combat;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills.Behaviors
{
    /// <summary>Firestorm evolution: a burning blast ring around the player on a fixed interval.</summary>
    public class FirestormBehavior : MonoBehaviour
    {
        private float _damage, _interval, _radius, _burnDps, _burnDuration;
        private float _nextAt;
        private LayerMask _enemyMask;

        public void Configure(float damage, float interval, float radius, float burnDps, float burnDuration)
        {
            _damage = damage;
            _interval = interval;
            _radius = radius;
            _burnDps = burnDps;
            _burnDuration = burnDuration;
            if (TryGetComponent<AutoAttackController>(out var attack)) _enemyMask = attack.EnemyMask;
        }

        private void Update()
        {
            if (Time.time < _nextAt || _interval <= 0f) return;
            _nextAt = Time.time + _interval;

            var burn = new StatusPayload { BurnDps = _burnDps, BurnDuration = _burnDuration };
            AreaDamage.DamageCircleWithStatus(transform.position, _radius, _enemyMask, _damage, 3f, gameObject, burn);
            // The ring is drawn by AreaDamage.AreaHit only when something is hit; the storm should always show.
            NinjaVillage.Gameplay.Vfx.Vfx.Explosion(transform.position, _radius, NinjaVillage.Gameplay.Vfx.Vfx.FireColor);
            Sfx.PlayAt(AudioCueIds.Burn, transform.position, 0.7f);
        }
    }
}
