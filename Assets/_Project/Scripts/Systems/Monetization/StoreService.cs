using System;
using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Systems.Monetization
{
    /// <summary>Raised when a real-money purchase finishes (any outcome) — the Shop screen refreshes on it.</summary>
    public readonly struct PurchaseResultEvent : IGameEvent
    {
        public readonly string ProductId;
        public readonly PurchaseOutcome Outcome;
        public PurchaseResultEvent(string productId, PurchaseOutcome outcome)
        {
            ProductId = productId;
            Outcome = outcome;
        }
    }

    /// <summary>
    /// Entry point for real-money purchases (EPIC 21). Uses Unity IAP when the package is installed
    /// (NV_UNITY_IAP, defined automatically by the asmdef's versionDefines) and the simulated store
    /// otherwise. Initialized once before the first scene.
    /// </summary>
    public static class StoreService
    {
        public static IStoreBackend Backend { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Backend != null) return;
#if NV_UNITY_IAP
            Backend = new UnityIapStoreBackend();
#else
            Backend = new SimulatedStoreBackend();
#endif
            Backend.Initialize();
        }

        public static bool IsReady => Backend != null && Backend.IsReady;
        public static string Price(string productId) => Backend != null ? Backend.GetPrice(productId) : StoreProducts.Get(productId)?.FallbackPrice;

        public static void Buy(string productId, Action<PurchaseOutcome> onComplete = null)
        {
            if (Backend == null)
            {
                onComplete?.Invoke(PurchaseOutcome.StoreUnavailable);
                return;
            }
            Backend.Purchase(productId, (outcome, id) =>
            {
                onComplete?.Invoke(outcome);
                // The IAP backend raises the event itself for every completion; the simulated one doesn't.
                if (!(Backend is SimulatedStoreBackend)) return;
                RaisePurchaseResult(id, outcome);
            });
        }

        public static void Restore(Action<bool, string> onComplete) =>
            (Backend ?? new SimulatedStoreBackend()).Restore(onComplete);

        internal static void RaisePurchaseResult(string productId, PurchaseOutcome outcome) =>
            EventBus<PurchaseResultEvent>.Raise(new PurchaseResultEvent(productId, outcome));

        public static string Describe(PurchaseOutcome outcome)
        {
            switch (outcome)
            {
                case PurchaseOutcome.Success: return "Purchase complete — thank you!";
                case PurchaseOutcome.Cancelled: return "Purchase cancelled";
                case PurchaseOutcome.Deferred: return "Purchase pending approval — you'll get it once approved";
                case PurchaseOutcome.StoreUnavailable: return "The store isn't available right now";
                default: return "Purchase failed — please try again";
            }
        }
    }
}
