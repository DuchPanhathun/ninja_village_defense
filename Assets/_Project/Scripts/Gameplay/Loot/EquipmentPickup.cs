using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Loot
{
    /// <summary>
    /// A world-space equipment drop. Flies toward the player when in magnet range,
    /// and applies its EquipmentDefinition's stat bonuses on contact.
    /// </summary>
    public class EquipmentPickup : MonoBehaviour
    {
        [SerializeField] private float baseMagnetRadius = 2.5f;
        [SerializeField] private float pickupDistance = 0.35f;
        [SerializeField] private float flySpeed = 8f;

        private EquipmentDefinition _definition;
        private PlayerStats _playerStats;

        public void Initialize(EquipmentDefinition definition)
        {
            _definition = definition;
        }

        private void Start()
        {
            if (PlayerReference.Instance != null)
                _playerStats = PlayerReference.Instance.GetComponent<PlayerStats>();
        }

        private void Update()
        {
            if (PlayerReference.Instance == null || _definition == null) return;

            Transform player = PlayerReference.Instance.PlayerTransform;
            float magnetRadius = baseMagnetRadius * (_playerStats != null ? _playerStats.XpMagnetRadiusMultiplier : 1f);
            float distance = Vector2.Distance(transform.position, player.position);

            if (distance <= pickupDistance)
            {
                Collect();
                return;
            }

            if (distance <= magnetRadius)
            {
                Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
                transform.position += (Vector3)(direction * flySpeed * Time.deltaTime);
            }
        }

        private void Collect()
        {
            if (_playerStats != null)
                _definition.Apply(_playerStats);
            Destroy(gameObject);
        }
    }
}
