using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Combat;
using NinjaVillage.Gameplay.Combat;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills.Behaviors
{
    /// <summary>
    /// Blinks the player away from danger when they take a hit (see <see cref="TeleportSkill"/>): jumps
    /// <c>distance</c> units directly away from the average position of nearby enemies, grants brief
    /// invulnerability, and leaves a glow at both ends.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class TeleportBehavior : MonoBehaviour
    {
        private const float ThreatScanRadius = 6f;

        private Health _health;
        private Rigidbody2D _body;
        private LayerMask _enemyMask;
        private float _cooldown;
        private float _distance;
        private float _invulnerability;
        private float _readyAt;

        public void Configure(float cooldown, float distance, float invulnerabilitySeconds)
        {
            _cooldown = cooldown;
            _distance = distance;
            _invulnerability = invulnerabilitySeconds;
        }

        private void Awake()
        {
            _health = GetComponent<Health>();
            _body = GetComponent<Rigidbody2D>();
            if (TryGetComponent<AutoAttackController>(out var attack)) _enemyMask = attack.EnemyMask;
        }

        private void OnEnable() => _health.OnDamaged += OnDamaged;
        private void OnDisable() => _health.OnDamaged -= OnDamaged;

        private void OnDamaged(float amount, float current, float max)
        {
            if (Time.time < _readyAt || !_health.IsAlive) return;
            _readyAt = Time.time + _cooldown;

            Vector2 from = transform.position;
            Vector2 away = TargetFinder.TryGetThreatCenter(from, ThreatScanRadius, _enemyMask, out var threat)
                ? (from - threat).normalized
                : Random.insideUnitCircle.normalized;
            if (away.sqrMagnitude < 0.01f) away = Vector2.up;
            Vector2 to = from + away * _distance;

            if (_body != null) _body.position = to;
            transform.position = to;
            _health.GrantInvulnerability(_invulnerability);

            var color = new Color(0.5f, 0.7f, 1f, 0.8f);
            NinjaVillage.Gameplay.Vfx.Vfx.Burst(from, color, 1.8f, 0.3f);
            NinjaVillage.Gameplay.Vfx.Vfx.Burst(to, color, 1.8f, 0.3f);
            Sfx.PlayAt(AudioCueIds.Teleport, to);
        }
    }
}
