using System;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Monetization
{
    /// <summary>
    /// Store used when Unity IAP isn't installed: every purchase succeeds instantly with a fake
    /// transaction id, so the whole grant/save flow can be exercised without a store account.
    /// Never ships in a release build with IAP installed (see <see cref="StoreService"/>).
    /// </summary>
    public sealed class SimulatedStoreBackend : IStoreBackend
    {
        private int _counter;

        public string Name => "Simulated";
        public bool IsReady => true;
        public void Initialize() { }

        public string GetPrice(string productId) => StoreProducts.Get(productId)?.FallbackPrice ?? "?";

        public void Purchase(string productId, Action<PurchaseOutcome, string> onComplete)
        {
            var result = PurchaseGrants.Grant(productId, $"sim_{DateTime.UtcNow.Ticks}_{_counter++}");
            SaveService.SaveNow();
            Debug.Log($"[Store:simulated] {productId} → {result}");
            onComplete?.Invoke(result == GrantResult.UnknownProduct ? PurchaseOutcome.Failed : PurchaseOutcome.Success, productId);
        }

        public void Restore(Action<bool, string> onComplete) => onComplete?.Invoke(true, null);
    }
}
