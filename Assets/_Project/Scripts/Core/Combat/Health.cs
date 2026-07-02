using System;
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
            _invulnerableUntil = 0f;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive) return;
            if (Time.time < _invulnerableUntil) return;
            if (DodgeRoll != null && DodgeRoll()) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage.Amount);

            if (invulnerabilityDuration > 0f)
                _invulnerableUntil = Time.time + invulnerabilityDuration;

            if (damage.KnockbackForce > 0f && optionalRigidbody != null)
                optionalRigidbody.AddForce(damage.KnockbackDirection * damage.KnockbackForce, ForceMode2D.Impulse);

            OnDamaged?.Invoke(damage.Amount, CurrentHealth, maxHealth);

            if (!IsAlive)
                OnDeath?.Invoke(this);
        }

        /// <summary>Grants temporary invulnerability (e.g. dash i-frames, skill effects).</summary>
        public void GrantInvulnerability(float seconds)
        {
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + seconds);
        }

        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }
    }
}
