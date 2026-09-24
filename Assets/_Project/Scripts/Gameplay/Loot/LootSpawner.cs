using NinjaVillage.Core;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Bosses;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Systems.Economy;
using System.Collections.Generic;
using UnityEngine;

namespace NinjaVillage.Gameplay.Loot
{
    /// <summary>
    /// Turns enemy deaths into physical coin and equipment drops. Applies the Gold
    /// Bonus and Lucky Drop skill modifiers. Equipment drops are rarity-weighted:
    /// the base drop chance is rolled first, then a rarity is selected by weight,
    /// then a random equipment of that rarity is chosen from the pool.
    /// </summary>
    public class LootSpawner : MonoBehaviour
    {
        [Header("Coins")]
        [SerializeField] private GameObject coinPickupPrefab;
        [SerializeField] private float scatterRadius = 0.5f;

        [Header("Equipment")]
        [SerializeField] private GameObject equipmentPickupPrefab;
        [SerializeField] private EquipmentDefinition[] equipmentPool;
        [Tooltip("Base chance per enemy kill that any equipment drops at all.")]
        [SerializeField] private float equipmentBaseDropChance = 0.04f;
        [Tooltip("Relative weights for Common / Rare / Epic / Legendary (in that order).")]
        [SerializeField] private float[] rarityWeights = { 60f, 25f, 12f, 3f };

        [Header("Boss chest")]
        [Tooltip("Every boss drops a chest (coins + a guaranteed-chance equipment piece).")]
        [SerializeField] private bool bossDropsChest = true;
        [SerializeField] private int bossChestMinCoins = 40;
        [SerializeField] private int bossChestMaxCoins = 120;

        private static readonly Rarity[] RarityOrder = { Rarity.Common, Rarity.Rare, Rarity.Epic, Rarity.Legendary };

        private void OnEnable()
        {
            EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
            EventBus<BossDefeatedEvent>.Subscribe(OnBossDefeated);
        }

        private void OnDisable()
        {
            EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
            EventBus<BossDefeatedEvent>.Unsubscribe(OnBossDefeated);
        }

        private void OnBossDefeated(BossDefeatedEvent evt)
        {
            if (bossDropsChest) ChestPickup.Create(evt.Position, bossChestMinCoins, bossChestMaxCoins, 1f);
        }

        /// <summary>The scene's list, or every catalog piece when none is assigned (so loot works with zero setup).</summary>
        private IReadOnlyList<EquipmentDefinition> Pool
        {
            get
            {
                if (equipmentPool != null && equipmentPool.Length > 0) return equipmentPool;
                var catalog = CatalogCache<EquipmentCatalog>.Get();
                return catalog != null ? catalog.All : System.Array.Empty<EquipmentDefinition>();
            }
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            SpawnCoins(evt);
            TrySpawnEquipment(evt.Position);
        }

        private void SpawnCoins(EnemyKilledEvent evt)
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
            var instance = PrefabPool.Get(coinPickupPrefab, position, Quaternion.identity);
            instance.GetComponent<CoinPickup>().Initialize(amount);
        }

        private void TrySpawnEquipment(Vector2 position)
        {
            var pool = Pool;
            if (pool.Count == 0) return;
            // Luck (Lucky Drop skill, Fortune blessing, Hawk pet) also improves equipment drops: +25% of it.
            float luck = 0f;
            if (PlayerReference.Instance != null && PlayerReference.Instance.TryGetComponent<PlayerStats>(out var stats))
                luck = stats.LuckyDropChanceBonus;
            if (Random.value > equipmentBaseDropChance + luck * 0.25f) return;

            Rarity rarity = RollRarity();
            var candidates = new List<EquipmentDefinition>();
            foreach (var def in pool)
            {
                if (def != null && def.Rarity == rarity)
                    candidates.Add(def);
            }

            // Fall back to any rarity if pool has nothing at the rolled tier.
            if (candidates.Count == 0)
            {
                foreach (var def in pool)
                    if (def != null) candidates.Add(def);
            }

            if (candidates.Count == 0) return;

            var chosen = candidates[Random.Range(0, candidates.Count)];
            Vector2 dropPosition = position + Random.insideUnitCircle * scatterRadius;
            if (equipmentPickupPrefab == null)
            {
                EquipmentPickup.Create(chosen, dropPosition);
                return;
            }
            var go = Instantiate(equipmentPickupPrefab, dropPosition, Quaternion.identity);
            if (go.TryGetComponent<EquipmentPickup>(out var pickup))
                pickup.Initialize(chosen);
        }

        private Rarity RollRarity()
        {
            float total = 0f;
            int len = Mathf.Min(rarityWeights.Length, RarityOrder.Length);
            for (int i = 0; i < len; i++) total += rarityWeights[i];

            float roll = Random.value * total;
            float cumulative = 0f;
            for (int i = 0; i < len; i++)
            {
                cumulative += rarityWeights[i];
                if (roll <= cumulative) return RarityOrder[i];
            }
            return Rarity.Common;
        }
    }
}
