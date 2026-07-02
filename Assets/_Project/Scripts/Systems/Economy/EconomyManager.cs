using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Systems.Economy
{
    /// <summary>
    /// Owns the player's persistent currency balances across the Village and Battle
    /// scenes. Auto-credits coins on enemy kills (scaled by the Gold Bonus utility
    /// skill) — the "Collect Loot" step of the core gameplay loop.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [SerializeField] private CurrencyWallet wallet = new();
        public CurrencyWallet Wallet => wallet;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable() => EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
        private void OnDisable() => EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (evt.CoinReward <= 0) return;

            float goldBonusMultiplier = 1f;
            if (PlayerReference.Instance != null && PlayerReference.Instance.TryGetComponent<PlayerStats>(out var stats))
                goldBonusMultiplier = stats.GoldBonusMultiplier;

            int amount = Mathf.RoundToInt(evt.CoinReward * goldBonusMultiplier);
            Add(CurrencyType.Coins, amount);
        }

        public void Add(CurrencyType type, int amount)
        {
            wallet.Add(type, amount);
            EventBus<CurrencyChangedEvent>.Raise(new CurrencyChangedEvent(type, wallet.Get(type)));
        }

        public bool TrySpend(CurrencyType type, int amount)
        {
            bool success = wallet.TrySpend(type, amount);
            if (success)
                EventBus<CurrencyChangedEvent>.Raise(new CurrencyChangedEvent(type, wallet.Get(type)));
            return success;
        }

        /// <summary>Overwrites the wallet from loaded save data.</summary>
        public void LoadFrom(CurrencyWallet savedWallet)
        {
            wallet.Coins = savedWallet.Coins;
            wallet.Gems = savedWallet.Gems;
        }
    }
}
