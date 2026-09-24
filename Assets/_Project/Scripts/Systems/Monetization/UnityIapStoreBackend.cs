#if NV_UNITY_IAP
using System;
using System.Collections.Generic;
using NinjaVillage.Systems.Save;
using UnityEngine;
using UnityEngine.Purchasing;

namespace NinjaVillage.Systems.Monetization
{
    /// <summary>
    /// Unity IAP 5 store (EPIC 21 "Gem purchases", "Starter packs", "Remove Ads purchase"). Follows the v5
    /// two-step flow: subscribe to every event before <c>Connect()</c>; on <c>OnPurchasePending</c> grant
    /// the reward (<see cref="PurchaseGrants"/>, de-duplicated by transaction id), save, and only then
    /// <c>ConfirmPurchase</c> — an unconfirmed purchase is re-delivered on next launch, so a crash can't
    /// lose a paid reward. Non-consumables (Remove Ads, Starter Pack) are re-applied from
    /// <c>OnPurchasesFetched</c> / Restore after a reinstall. In the Editor, Unity IAP uses its fake store.
    /// </summary>
    public sealed class UnityIapStoreBackend : IStoreBackend
    {
        private StoreController _store;
        private bool _connected;
        private bool _productsReady;
        private readonly Dictionary<string, Action<PurchaseOutcome, string>> _pendingCallbacks = new();

        public string Name => "Unity IAP";
        public bool IsReady => _connected && _productsReady;

        public async void Initialize()
        {
            _store = UnityIAPServices.StoreController();

            // Every event subscribed BEFORE Connect: pending purchases from a previous session fire right away.
            _store.OnStoreConnected += OnStoreConnected;
            _store.OnStoreDisconnected += failure => { _connected = false; Debug.LogWarning($"[IAP] Store disconnected: {failure.Message}"); };
            _store.OnProductsFetched += products => { _productsReady = true; Debug.Log($"[IAP] {products.Count} products ready"); };
            _store.OnProductsFetchFailed += failure => Debug.LogWarning($"[IAP] Product fetch failed: {failure.FailureReason}");
            _store.OnPurchasesFetched += OnPurchasesFetched;
            _store.OnPurchasesFetchFailed += failure => Debug.LogWarning($"[IAP] Purchase fetch failed: {failure.Message}");
            _store.OnPurchasePending += OnPurchasePending;
            _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _store.OnPurchaseFailed += OnPurchaseFailed;
            _store.OnPurchaseDeferred += OnPurchaseDeferred;

            try
            {
                await _store.Connect();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[IAP] Connect failed: {e.Message}");
            }
        }

        private void OnStoreConnected()
        {
            _connected = true;
            var definitions = new List<ProductDefinition>();
            foreach (var product in StoreProducts.All)
                definitions.Add(new ProductDefinition(product.Id,
                    product.Type == StoreProductType.Consumable ? ProductType.Consumable : ProductType.NonConsumable));
            _store.FetchProducts(definitions);
            _store.FetchPurchases();
        }

        public string GetPrice(string productId)
        {
            var product = _store != null && _productsReady ? _store.GetProductById(productId) : null;
            if (product != null && product.metadata != null && !string.IsNullOrEmpty(product.metadata.localizedPriceString))
                return product.metadata.localizedPriceString;
            return StoreProducts.Get(productId)?.FallbackPrice ?? "?";
        }

        public void Purchase(string productId, Action<PurchaseOutcome, string> onComplete)
        {
            if (!IsReady)
            {
                onComplete?.Invoke(PurchaseOutcome.StoreUnavailable, productId);
                return;
            }
            var product = _store.GetProductById(productId);
            if (product == null || !product.availableToPurchase)
            {
                onComplete?.Invoke(PurchaseOutcome.StoreUnavailable, productId);
                return;
            }
            _pendingCallbacks[productId] = onComplete;
            _store.PurchaseProduct(product);
        }

        public void Restore(Action<bool, string> onComplete)
        {
            if (_store == null || !_connected)
            {
                onComplete?.Invoke(false, "Store not connected");
                return;
            }
            // Each restored purchase arrives through OnPurchasePending.
            _store.RestoreTransactions((success, error) => onComplete?.Invoke(success, error));
        }

        private void OnPurchasePending(PendingOrder order)
        {
            string transactionId = order.Info != null ? order.Info.TransactionID : null;
            var granted = new List<string>();
            foreach (var item in order.CartOrdered.Items())
            {
                string productId = item.Product?.definition?.id;
                if (string.IsNullOrEmpty(productId)) continue;
                PurchaseGrants.Grant(productId, transactionId);
                granted.Add(productId);
            }

            try
            {
                SaveService.SaveNow();
            }
            catch (Exception e)
            {
                // Not confirming: the store re-delivers the order next launch, and the ledger prevents a double grant.
                Debug.LogError($"[IAP] Save failed, purchase left pending: {e.Message}");
                return;
            }

            _store.ConfirmPurchase(order);
            foreach (var productId in granted) Complete(productId, PurchaseOutcome.Success);
        }

        private static void OnPurchaseConfirmed(Order order)
        {
            if (order is FailedOrder failed)
                Debug.LogWarning($"[IAP] Confirmation failed: {failed.FailureReason} - {failed.Details}");
        }

        private void OnPurchaseFailed(FailedOrder failed)
        {
            var outcome = failed.FailureReason == PurchaseFailureReason.UserCancelled ? PurchaseOutcome.Cancelled : PurchaseOutcome.Failed;
            foreach (var item in failed.CartOrdered.Items())
                Complete(item.Product?.definition?.id, outcome);
            if (outcome == PurchaseOutcome.Failed)
                Debug.LogWarning($"[IAP] Purchase failed: {failed.FailureReason} - {failed.Details}");
        }

        private void OnPurchaseDeferred(DeferredOrder deferred)
        {
            // Ask-to-Buy / pending payment: grant nothing now — OnPurchasePending fires once approved.
            foreach (var item in deferred.CartOrdered.Items())
                Complete(item.Product?.definition?.id, PurchaseOutcome.Deferred);
        }

        /// <summary>Re-applies owned non-consumables (e.g. after a reinstall without a cloud save).</summary>
        private void OnPurchasesFetched(Orders orders)
        {
            bool changed = false;
            foreach (var confirmed in orders.ConfirmedOrders)
            {
                foreach (var item in confirmed.CartOrdered.Items())
                {
                    string productId = item.Product?.definition?.id;
                    var product = StoreProducts.Get(productId);
                    if (product == null || product.Type != StoreProductType.NonConsumable) continue;
                    changed |= PurchaseGrants.Grant(productId, confirmed.Info?.TransactionID) == GrantResult.Granted;
                }
            }
            if (changed) SaveService.SaveNow();
        }

        private void Complete(string productId, PurchaseOutcome outcome)
        {
            if (string.IsNullOrEmpty(productId)) return;
            if (_pendingCallbacks.TryGetValue(productId, out var callback))
            {
                _pendingCallbacks.Remove(productId);
                callback?.Invoke(outcome, productId);
            }
            StoreService.RaisePurchaseResult(productId, outcome);
        }
    }
}
#endif
