using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Ultimates
{
    /// <summary>
    /// Lives on the player. Kills build charge; when full and off cooldown, the
    /// on-screen ultimate button calls <see cref="TryActivate"/>.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class UltimateController : MonoBehaviour
    {
        [SerializeField] private UltimateDefinition equippedUltimate;
        [Tooltip("Charge percent gained per enemy kill (100 = full).")]
        [SerializeField] private float chargePerKill = 4f;

        private float _charge; // 0..100
        private float _cooldownUntil;
        private PlayerStats _stats;
        private AutoAttackController _autoAttack;

        public UltimateDefinition EquippedUltimate => equippedUltimate;
        public float ChargeNormalized => _charge / 100f;
        public bool IsReady => equippedUltimate != null && _charge >= 100f && Time.time >= _cooldownUntil;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _autoAttack = GetComponent<AutoAttackController>();
        }

        private void OnEnable() => EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
        private void OnDisable() => EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);

        public void Equip(UltimateDefinition ultimate) => equippedUltimate = ultimate;

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (_charge >= 100f) return;
            _charge = Mathf.Min(100f, _charge + chargePerKill);
            EventBus<UltimateChargeChangedEvent>.Raise(new UltimateChargeChangedEvent(ChargeNormalized));
        }

        /// <summary>Hook the on-screen ultimate button's onClick to this.</summary>
        public void TryActivate()
        {
            if (!IsReady) return;

            _charge = 0f;
            _cooldownUntil = Time.time + equippedUltimate.CooldownSeconds;

            LayerMask enemyMask = _autoAttack != null ? _autoAttack.EnemyMask : default;
            equippedUltimate.Activate(new UltimateContext(this, transform, _stats, enemyMask));

            EventBus<UltimateActivatedEvent>.Raise(new UltimateActivatedEvent(equippedUltimate));
            EventBus<UltimateChargeChangedEvent>.Raise(new UltimateChargeChangedEvent(0f));
        }
    }
}
