using UnityEngine;

namespace NinjaVillage.Core.Combat
{
    /// <summary>
    /// The on-hit status effects a projectile can carry. Skills (Fire Blade,
    /// Poison Kunai) add these to PlayerStats; AutoAttackController copies them
    /// onto each projectile it fires.
    /// </summary>
    public struct StatusPayload
    {
        public float BurnDps;
        public float BurnDuration;
        public float PoisonDps;
        public float PoisonDuration;

        public bool HasAny => BurnDps > 0f || PoisonDps > 0f;
    }

    /// <summary>
    /// Receives and ticks status effects (burn/poison damage-over-time, slow, stun)
    /// on any entity with a <see cref="Health"/>. Movement scripts read
    /// <see cref="MoveSpeedMultiplier"/> and <see cref="IsStunned"/> each frame.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class StatusEffectReceiver : MonoBehaviour
    {
        private Health _health;

        private float _burnDps, _burnUntil;
        private float _poisonDps, _poisonUntil;
        private float _slowMultiplier = 1f;
        private float _slowUntil;
        private float _stunUntil;

        public bool IsStunned => Time.time < _stunUntil;
        public float MoveSpeedMultiplier => Time.time < _slowUntil ? _slowMultiplier : 1f;
        public bool IsBurning => Time.time < _burnUntil;
        public bool IsPoisoned => Time.time < _poisonUntil;

        private void Awake() => _health = GetComponent<Health>();

        public void Apply(in StatusPayload payload)
        {
            if (payload.BurnDps > 0f) ApplyBurn(payload.BurnDps, payload.BurnDuration);
            if (payload.PoisonDps > 0f) ApplyPoison(payload.PoisonDps, payload.PoisonDuration);
        }

        /// <summary>Re-applying keeps the strongest DPS and refreshes the duration.</summary>
        public void ApplyBurn(float dps, float duration)
        {
            _burnDps = Mathf.Max(_burnDps * (IsBurning ? 1f : 0f), dps);
            _burnUntil = Time.time + duration;
        }

        public void ApplyPoison(float dps, float duration)
        {
            _poisonDps = Mathf.Max(_poisonDps * (IsPoisoned ? 1f : 0f), dps);
            _poisonUntil = Time.time + duration;
        }

        /// <summary>multiplier is the remaining speed fraction, e.g. 0.5 = half speed. Strongest slow wins.</summary>
        public void ApplySlow(float multiplier, float duration)
        {
            multiplier = Mathf.Clamp01(multiplier);
            if (!(Time.time < _slowUntil) || multiplier < _slowMultiplier)
                _slowMultiplier = multiplier;
            _slowUntil = Mathf.Max(_slowUntil, Time.time + duration);
        }

        public void ApplyStun(float duration)
        {
            _stunUntil = Mathf.Max(_stunUntil, Time.time + duration);
        }

        private void Update()
        {
            if (!_health.IsAlive) return;

            float dot = 0f;
            if (IsBurning) dot += _burnDps;
            if (IsPoisoned) dot += _poisonDps;
            if (dot <= 0f) return;

            // DoT bypasses i-frames/dodge/shields (a burn keeps burning) but still
            // goes through Health so death handling stays in one place.
            _health.TakeDamageOverTime(dot * Time.deltaTime);
        }
    }
}
