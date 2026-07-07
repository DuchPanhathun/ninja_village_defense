using NinjaVillage.Gameplay.Player;
using NinjaVillage.Systems.Economy;
using UnityEngine;

namespace NinjaVillage.Gameplay.Loot
{
    /// <summary>
    /// A chest that grants a bundle of coins + optional equipment on interaction.
    /// Touching the player triggers the reward automatically.
    /// </summary>
    public class ChestPickup : MonoBehaviour
    {
        [Header("Rewards")]
        [SerializeField] private int minCoins = 15;
        [SerializeField] private int maxCoins = 50;
        [SerializeField] private EquipmentDefinition[] possibleEquipment;
        [Tooltip("Chance (0-1) to drop one piece of equipment from possibleEquipment.")]
        [SerializeField] private float equipmentDropChance = 0.6f;
        [SerializeField] private GameObject equipmentPickupPrefab;

        private bool _opened;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_opened) return;
            if (!other.CompareTag("Player")) return;

            Open(other.GetComponent<PlayerStats>());
        }

        private void Open(PlayerStats playerStats)
        {
            _opened = true;

            // Coins
            int coins = Random.Range(minCoins, maxCoins + 1);
            if (playerStats != null)
                coins = Mathf.Max(1, Mathf.RoundToInt(coins * playerStats.GoldBonusMultiplier));

            if (EconomyManager.Instance != null)
                EconomyManager.Instance.Add(CurrencyType.Coins, coins);

            // Equipment
            if (possibleEquipment != null && possibleEquipment.Length > 0 &&
                Random.value <= equipmentDropChance && equipmentPickupPrefab != null)
            {
                var chosen = possibleEquipment[Random.Range(0, possibleEquipment.Length)];
                var go = Instantiate(equipmentPickupPrefab, transform.position + (Vector3)(Random.insideUnitCircle * 0.5f), Quaternion.identity);
                if (go.TryGetComponent<EquipmentPickup>(out var pickup))
                    pickup.Initialize(chosen);
            }

            Destroy(gameObject);
        }
    }
}
