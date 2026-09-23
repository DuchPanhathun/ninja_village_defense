using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Economy
{
    /// <summary>
    /// Owns the player's persistent currency balances across the Main Menu, Village and
    /// Battle scenes. Coins are earned through physical <c>CoinPickup</c> drops spawned by
    /// <c>LootSpawner</c> — this manager just holds and mutates balances.
    ///
    /// The wallet IS the save's wallet (<see cref="SaveService"/>), so every change is
    /// persisted without any copy-back step. One instance is created automatically before
    /// the first scene loads; a copy placed in a scene is harmless (it destroys itself).
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        public CurrencyWallet Wallet => SaveService.Data.Wallet;

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

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public int Get(CurrencyType type) => Wallet.Get(type);

        public bool CanAfford(CurrencyType type, int amount) => Wallet.Get(type) >= amount;

        public void Add(CurrencyType type, int amount)
        {
            if (amount <= 0) return;
            Wallet.Add(type, amount);
            SaveService.MarkDirty();
            EventBus<CurrencyChangedEvent>.Raise(new CurrencyChangedEvent(type, Wallet.Get(type)));
        }

        public bool TrySpend(CurrencyType type, int amount)
        {
            bool success = Wallet.TrySpend(type, amount);
            if (success)
            {
                // Spending is a player decision worth persisting right away.
                SaveService.SaveNow();
                EventBus<CurrencyChangedEvent>.Raise(new CurrencyChangedEvent(type, Wallet.Get(type)));
            }
            return success;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            new GameObject("[EconomyManager]").AddComponent<EconomyManager>();
        }
    }
}
