using NinjaVillage.Core.Audio;
using UnityEngine;

namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// Monkey — "Throws bananas (stuns enemies)". Every <c>Cooldown</c> seconds it lobs a
    /// <see cref="BananaProjectile"/> at the nearest enemy within <c>Range</c>; the splash deals
    /// <c>Power</c> damage and stuns for <c>EffectDuration</c> seconds. Uses the definition's
    /// ability prefab as the banana visual if set.
    /// </summary>
    public class MonkeyBananaAbility : PetAbility
    {
        [SerializeField] private float flightTime = 0.55f;
        [SerializeField] private float arcHeight = 1.2f;
        [SerializeField] private float splashRadius = 0.9f;
        [Tooltip("Retry delay when nothing is in range.")]
        [SerializeField] private float idleRetry = 0.25f;

        private float _cooldown;

        protected override void OnInitialized() => _cooldown = 1f;

        private void Update()
        {
            if (!IsReady || Owner == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            _cooldown -= dt;
            if (_cooldown > 0f) return;

            var target = PetCombat.FindNearestEnemy(transform.position, Stats.Range, EnemyMask);
            if (target == null)
            {
                _cooldown = idleRetry;
                return;
            }

            Throw(target);
            _cooldown = Stats.Cooldown;
        }

        private void Throw(Transform target)
        {
            Vector2 origin = transform.position;
            Controller.FaceToward(target.position);

            GameObject go = Definition != null && Definition.AbilityPrefab != null
                ? Instantiate(Definition.AbilityPrefab, origin, Quaternion.identity)
                : BananaProjectile.CreatePlaceholder(origin);

            if (!go.TryGetComponent<BananaProjectile>(out var banana))
                banana = go.AddComponent<BananaProjectile>();

            banana.Launch(origin, target, flightTime, arcHeight, ScaledDamage(Stats.Power), Stats.EffectDuration,
                splashRadius, EnemyMask, gameObject);
            Sfx.PlayAt(AudioCueIds.ShurikenThrow, origin, 0.5f);
        }
    }
}
