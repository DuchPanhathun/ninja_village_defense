using System;
using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Core.Combat
{
    /// <summary>
    /// Shared health pool used by both the player and every enemy. Applies damage,
    /// optional knockback (if a Rigidbody2D is present) and fires local C# events
    /// that owning scripts (PlayerController, EnemyController) translate into
    /// global <see cref="Events.EventBus{T}"/> events with context they alone know
    /// (XP reward, coin reward, etc.).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float invulnerabilityDuration = 0f;
        [SerializeField] private Rigidbody2D optionalRigidbody;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        /// <summary>Absorbs damage before health. Granted by the Shield skill and similar effects.</summary>
        public float CurrentShield { get; private set; }
        public bool IsAlive => CurrentHealth > 0f;
        public Transform Transform => transform;

        private float _invulnerableUntil;

        /// <summary>amount taken, current health, max health.</summary>
        public event Action<float, float, float> OnDamaged;
        public event Action<Health> OnDeath;

        /// <summary>
        /// Optional hook checked before any damage is applied — return true to dodge
        /// the hit entirely. Lets PlayerStats' Dodge Chance skill plug in without
        /// this generic component knowing about player-specific stats.
        /// </summary>
        public Func<bool> DodgeRoll;

        /// <summary>
        /// Optional hook that scales a direct hit before shields/health (e.g. the player's
        /// DamageReduction stat). Not applied to damage-over-time.
        /// </summary>
        public Func<float, float> IncomingDamageModifier;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            if (optionalRigidbody == null)
                optionalRigidbody = GetComponent<Rigidbody2D>();
        }

        /// <summary>Resets health to max — used when (re)spawning or starting a new run.</summary>
        public void ResetHealth(float? newMaxHealth = null)
        {
            if (newMaxHealth.HasValue)
                maxHealth = newMaxHealth.Value;
            CurrentHealth = maxHealth;
            CurrentShield = 0f;
            _invulnerableUntil = 0f;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive) return;
            if (Time.time < _invulnerableUntil) return;
            if (DodgeRoll != null && DodgeRoll()) return;

            // Shield absorbs first; only the remainder reaches health.
            float remaining = IncomingDamageModifier != null ? Mathf.Max(0f, IncomingDamageModifier(damage.Amount)) : damage.Amount;
            if (CurrentShield > 0f)
            {
                float absorbed = Mathf.Min(CurrentShield, remaining);
                CurrentShield -= absorbed;
                remaining -= absorbed;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - remaining);

            if (invulnerabilityDuration > 0f)
                _invulnerableUntil = Time.time + invulnerabilityDuration;

            if (damage.KnockbackForce > 0f && optionalRigidbody != null)
                optionalRigidbody.AddForce(damage.KnockbackDirection * damage.KnockbackForce, ForceMode2D.Impulse);

            OnDamaged?.Invoke(damage.Amount, CurrentHealth, maxHealth);
            EventBus<EntityDamagedEvent>.Raise(new EntityDamagedEvent(transform.position, damage.Amount, damage.IsCritical));

            if (!IsAlive)
                OnDeath?.Invoke(this);
        }

        /// <summary>
        /// Damage-over-time entry point used by <see cref="StatusEffectReceiver"/>.
        /// Bypasses i-frames, dodge and shields (a burn keeps burning) and does not
        /// spam the global damage-number event with per-frame slivers.
        /// </summary>
        public void TakeDamageOverTime(float amount)
        {
            if (!IsAlive || amount <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            OnDamaged?.Invoke(amount, CurrentHealth, maxHealth);

            if (!IsAlive)
                OnDeath?.Invoke(this);
        }

        /// <summary>Adds shield points up to <paramref name="cap"/> (skills pass their own cap so stacking stays bounded).</summary>
        public void GainShield(float amount, float cap)
        {
            CurrentShield = Mathf.Min(cap, CurrentShield + amount);
        }

        /// <summary>Grants temporary invulnerability (e.g. dash i-frames, skill effects).</summary>
        public void GrantInvulnerability(float seconds)
        {
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + seconds);
        }

        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        /// <summary>Brings a dead entity back with <paramref name="healthFraction"/> of max health (revive ads).</summary>
        public void Revive(float healthFraction)
        {
            if (IsAlive) return;
            CurrentHealth = Mathf.Max(1f, maxHealth * Mathf.Clamp01(healthFraction));
            CurrentShield = 0f;
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }
    }
}
