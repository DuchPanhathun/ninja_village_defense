using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Combat;
using UnityEngine;

namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// Wolf — "Bites enemies". Hunts the nearest enemy within <c>Range</c> of the player, runs up
    /// to it and bites for <c>Power</c> damage (plus inherited owner damage) every <c>Cooldown</c>
    /// seconds through <see cref="IDamageable"/>. Returns to its orbit slot when nothing is in
    /// range. Also used for the Beast Ninja's wolf pack.
    /// </summary>
    public class WolfBiteAbility : PetAbility
    {
        [SerializeField] private float biteReach = 0.9f;
        [SerializeField] private float knockback = 1.5f;
        [SerializeField, Range(0f, 1f)] private float critChance = 0.1f;
        [SerializeField] private float critMultiplier = 1.5f;
        [SerializeField] private float retargetInterval = 0.25f;
        [SerializeField] private float chaseSpeedMultiplier = 1.4f;

        private Transform _target;
        private IDamageable _targetDamageable;
        private float _biteCooldown;
        private float _retargetTimer;

        protected override void OnInitialized()
        {
            _target = null;
            _targetDamageable = null;
            _biteCooldown = 0f;
            _retargetTimer = 0f;
        }

        private void Update()
        {
            if (!IsReady) return;
            var owner = Owner;
            if (owner == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            _biteCooldown -= dt;
            _retargetTimer -= dt;

            Vector2 ownerPosition = owner.position;
            float leash = Stats.Range * 1.3f;

            if (_target != null)
            {
                bool alive = _targetDamageable != null && _targetDamageable.IsAlive;
                bool tooFar = ((Vector2)_target.position - ownerPosition).sqrMagnitude > leash * leash;
                if (!alive || tooFar) ClearTarget();
            }
            else if (_targetDamageable != null)
            {
                ClearTarget(); // target was destroyed
            }

            if (_target == null && _retargetTimer <= 0f)
            {
                _retargetTimer = retargetInterval;
                var found = PetCombat.FindNearestEnemy(ownerPosition, Stats.Range, EnemyMask);
                if (found != null && found.TryGetComponent<IDamageable>(out var damageable))
                {
                    _target = found;
                    _targetDamageable = damageable;
                }
            }

            if (_target == null)
            {
                if (Controller.HasMoveTarget) Controller.ClearMoveTarget();
                return;
            }

            Vector2 position = transform.position;
            Vector2 targetPosition = _target.position;
            Vector2 fromTarget = position - targetPosition;
            Vector2 approach = fromTarget.sqrMagnitude > 0.0001f
                ? targetPosition + fromTarget.normalized * (biteReach * 0.6f)
                : targetPosition;
            Controller.SetMoveTarget(approach, chaseSpeedMultiplier);

            if (_biteCooldown <= 0f && (targetPosition - position).sqrMagnitude <= biteReach * biteReach)
                Bite(position, targetPosition);
        }

        private void Bite(Vector2 position, Vector2 targetPosition)
        {
            _biteCooldown = Stats.Cooldown;

            bool isCritical = Random.value < critChance;
            float damage = ScaledDamage(Stats.Power) * (isCritical ? critMultiplier : 1f);
            Vector2 direction = targetPosition - position;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;

            _targetDamageable.TakeDamage(new DamageInfo(damage, isCritical, direction, knockback, gameObject));
            Sfx.PlayAt(AudioCueIds.EnemyHit, targetPosition, 0.6f);
        }

        private void ClearTarget()
        {
            _target = null;
            _targetDamageable = null;
        }
    }
}
