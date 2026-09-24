using System;

namespace NinjaVillage.Systems.Monetization
{
    public enum PurchaseOutcome
    {
        Success,
        Cancelled,
        Failed,
        /// <summary>Waiting for approval (Ask-to-Buy / pending payment) — granted later when approved.</summary>
        Deferred,
        StoreUnavailable
    }

    /// <summary>
    /// Real-money store abstraction: <see cref="UnityIapStoreBackend"/> (Unity IAP 5; the Editor uses its
    /// fake store) or <see cref="SimulatedStoreBackend"/> when the IAP package isn't installed.
    /// Implementations call <see cref="PurchaseGrants.Grant"/> and save BEFORE confirming a purchase.
    /// </summary>
    public interface IStoreBackend
    {
        string Name { get; }
        bool IsReady { get; }
        void Initialize();
        /// <summary>Localized price, or the product's fallback price while the store isn't ready.</summary>
        string GetPrice(string productId);
        void Purchase(string productId, Action<PurchaseOutcome, string> onComplete);
        void Restore(Action<bool, string> onComplete);
    }
}
