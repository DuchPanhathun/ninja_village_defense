using NinjaVillage.Core.Combat;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills.Behaviors
{
    /// <summary>
    /// Granted by the Shield skill: regenerates shield points (absorbed before HP)
    /// on an interval, up to a cap.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class ShieldRegenBehavior : MonoBehaviour
    {
        private Health _health;
        private float _shieldPerTick;
        private float _shieldCap;
        private float _interval;
        private float _nextTickAt;

        public void Configure(float shieldPerTick, float shieldCap, float interval)
        {
            _shieldPerTick = shieldPerTick;
            _shieldCap = shieldCap;
            _interval = interval;
        }

        private void Awake() => _health = GetComponent<Health>();

        private void Update()
        {
            if (Time.time < _nextTickAt || !_health.IsAlive) return;

            _health.GainShield(_shieldPerTick, _shieldCap);
            _nextTickAt = Time.time + _interval;
        }
    }
}
