using NinjaVillage.Core.Audio;
using UnityEngine;

namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// Dragon — "Breathes fire". Every <c>Cooldown</c> seconds it turns to the nearest enemy within
    /// <c>Range</c> and breathes a cone: <c>Power</c> damage to every enemy inside plus a burn of
    /// <see cref="burnDpsFraction"/> × damage per second for <c>EffectDuration</c> seconds
    /// (<c>StatusEffectReceiver.ApplyBurn</c>). Uses the ability prefab as the breath visual if
    /// set, otherwise a fading placeholder cone.
    /// </summary>
    public class DragonFireBreathAbility : PetAbility
    {
        [SerializeField, Range(5f, 90f)] private float coneHalfAngle = 30f;
        [SerializeField] private float burnDpsFraction = 0.3f;
        [SerializeField] private float knockback = 0.8f;
        [SerializeField] private float idleRetry = 0.25f;
        [SerializeField] private Color breathColor = new(1f, 0.45f, 0.1f, 0.85f);

        private float _cooldown;

        protected override void OnInitialized() => _cooldown = 1.5f;

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

            Breathe(target.position);
            _cooldown = Stats.Cooldown;
        }

        private void Breathe(Vector2 targetPosition)
        {
            Vector2 origin = transform.position;
            Vector2 direction = targetPosition - origin;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            Controller.FaceToward(targetPosition);

            float damage = ScaledDamage(Stats.Power);
            PetCombat.HitArea(origin, Stats.Range, EnemyMask, damage, knockback, gameObject,
                direction, coneHalfAngle, 0f, damage * burnDpsFraction, Stats.EffectDuration);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (Definition != null && Definition.AbilityPrefab != null)
            {
                var fx = Instantiate(Definition.AbilityPrefab, origin, Quaternion.Euler(0f, 0f, angle));
                if (!fx.TryGetComponent<PetTimedFade>(out var fade)) fade = fx.AddComponent<PetTimedFade>();
                fade.Configure(0.35f, 1.1f);
            }
            else
            {
                PetTimedFade.Spawn(PetPlaceholderSprites.Wedge, origin, angle, Vector3.one * Stats.Range, breathColor, 0.35f, 1.1f);
            }

            Sfx.PlayAt(AudioCueIds.Burn, origin, 0.7f);
        }
    }
}
