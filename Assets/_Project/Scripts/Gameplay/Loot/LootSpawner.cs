using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Loot
{
    /// <summary>
    /// Turns enemy deaths into physical coin drops — the "Collect Loot" step of the
    /// core loop. Applies the Gold Bonus multiplier and the Lucky Drop skill
    /// (chance to double the drop).
    /// </summary>
    public class LootSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject coinPickupPrefab;
        [Tooltip("Coins scatter this far from the death position.")]
        [SerializeField] private float scatterRadius = 0.5f;

        private void OnEnable() => EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
        private void OnDisable() => EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (coinPickupPrefab == null || evt.CoinReward <= 0) return;

            float goldMultiplier = 1f;
            float luckyChance = 0f;
            if (PlayerReference.Instance != null && PlayerReference.Instance.TryGetComponent<PlayerStats>(out var stats))
            {
                goldMultiplier = stats.GoldBonusMultiplier;
                luckyChance = stats.LuckyDropChanceBonus;
            }

            int amount = Mathf.Max(1, Mathf.RoundToInt(evt.CoinReward * goldMultiplier));
            if (Random.value < luckyChance)
                amount *= 2;

            Vector2 position = evt.Position + Random.insideUnitCircle * scatterRadius;
            var instance = Instantiate(coinPickupPrefab, position, Quaternion.identity);
            instance.GetComponent<CoinPickup>().Initialize(amount);
        }
    }
}
