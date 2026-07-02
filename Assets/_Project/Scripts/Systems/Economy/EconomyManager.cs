using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Systems.Economy
{
    /// <summary>
    /// Owns the player's persistent currency balances across the Village and Battle
    /// scenes. Coins are earned through physical <c>CoinPickup</c> drops spawned by
    /// <c>LootSpawner</c> — this manager just holds and mutates balances.
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
