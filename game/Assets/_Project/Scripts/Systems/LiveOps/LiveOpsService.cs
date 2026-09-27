using System;
using System.Collections.Generic;
using NinjaVillage.Core.Config;
using NinjaVillage.Core.Data;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.LiveOps
{
    /// <summary>Raised when the active event, its missions or limited offers change.</summary>
    public readonly struct LiveOpsChangedEvent : IGameEvent { }

    /// <summary>
    /// Seasonal events, event missions and the limited-time shop (EPIC 20). The active event is the one
    /// whose window contains <see cref="GameClock.UtcNow"/> and that isn't disabled remotely
    /// (<c>event_&lt;id&gt;_enabled</c>). Mission progress resets whenever a different event becomes active.
    /// </summary>
    public static class LiveOpsService
    {
        public static EventCatalog Events => CatalogLoader.Load<EventCatalog>();
        public static LimitedOfferCatalog Offers => CatalogLoader.Load<LimitedOfferCatalog>();

        private static LiveOpsSaveData Data
        {
            get
            {
                var liveOps = SaveService.Data.LiveOps;
                liveOps.EventMissions ??= new List<QuestProgress>();
                liveOps.LimitedOfferPurchases ??= new List<IdLevelEntry>();
                return liveOps;
            }
        }

        public static bool IsEnabled(EventDefinition evt) =>
            evt != null && RemoteValues.GetBool($"event_{evt.Id}_enabled", true);

        public static EventDefinition ActiveEvent
        {
            get
            {
                var catalog = Events;
                if (catalog == null) return null;
                DateTime now = GameClock.UtcNow;
                foreach (var evt in catalog.All)
                    if (evt != null && evt.IsInWindow(now) && IsEnabled(evt)) return evt;
                return null;
            }
        }

        public static EventDefinition NextEvent
        {
            get
            {
                var catalog = Events;
                if (catalog == null) return null;
                DateTime now = GameClock.UtcNow;
                EventDefinition best = null;
                foreach (var evt in catalog.All)
                {
                    if (evt == null || !IsEnabled(evt) || !evt.StartUtc.HasValue || evt.StartUtc.Value <= now) continue;
                    if (best == null || evt.StartUtc.Value < best.StartUtc.Value) best = evt;
                }
                return best;
            }
        }

        /// <summary>Syncs the saved mission list with the active event (reset on change). Returns the active event.</summary>
        public static EventDefinition EnsureEvent()
        {
            var active = ActiveEvent;
            string activeId = active != null ? active.Id : null;
            var data = Data;
            if (data.ActiveEventId == activeId) return active;

            data.ActiveEventId = activeId;
            data.EventMissions.Clear();
            if (active != null)
                foreach (var mission in active.Missions)
                    if (mission != null && !string.IsNullOrEmpty(mission.id))
                        data.EventMissions.Add(new QuestProgress { Id = mission.id });
            SaveService.MarkDirty();
            EventBus<LiveOpsChangedEvent>.Raise(new LiveOpsChangedEvent());
            return active;
        }

        public static IReadOnlyList<QuestProgress> MissionProgress => Data.EventMissions;

        public static bool TryClaimMission(string missionId)
        {
            var active = EnsureEvent();
            if (active == null) return false;
            var mission = active.GetMission(missionId);
            var progress = DailySaveData.Find(Data.EventMissions, missionId);
            if (mission == null || progress == null || !progress.Completed || progress.Claimed) return false;

            progress.Claimed = true;
            LiveOpsRewards.GrantAll(mission.rewards, $"mission_{active.Id}_{missionId}");
            EventBus<LiveOpsChangedEvent>.Raise(new LiveOpsChangedEvent());
            return true;
        }

        // ---------------------------------------------------------------- limited-time shop

        public static bool IsOfferAvailable(LimitedOfferDefinition offer)
        {
            if (offer == null) return false;
            if (!string.IsNullOrEmpty(offer.EventId))
            {
                var active = ActiveEvent;
                return active != null && active.Id == offer.EventId;
            }
            return LiveOpsRules.IsWithin(GameClock.UtcNow, offer.StartUtc, offer.DurationDays);
        }

        public static List<LimitedOfferDefinition> AvailableOffers()
        {
            var list = new List<LimitedOfferDefinition>();
            var catalog = Offers;
            if (catalog == null) return list;
            foreach (var offer in catalog.All)
                if (IsOfferAvailable(offer)) list.Add(offer);
            return list;
        }

        public static int Purchases(LimitedOfferDefinition offer) => offer != null ? Data.LimitedOfferPurchases.GetLevel(offer.Id) : 0;

        /// <summary>When the offer disappears: its event's end, or its own window's end.</summary>
        public static DateTime? OfferEndsUtc(LimitedOfferDefinition offer)
        {
            if (offer == null) return null;
            if (!string.IsNullOrEmpty(offer.EventId))
            {
                var active = ActiveEvent;
                return active != null && active.Id == offer.EventId ? active.EndUtc : null;
            }
            return offer.EndUtc;
        }

        public enum OfferResult { Ok, Unavailable, SoldOut, NotEnoughCurrency }

        public static OfferResult CheckOffer(LimitedOfferDefinition offer)
        {
            if (!IsOfferAvailable(offer)) return OfferResult.Unavailable;
            if (!LiveOpsRules.CanPurchase(Purchases(offer), offer.PurchaseLimit)) return OfferResult.SoldOut;
            if (!CurrencyService.CanAfford(offer.Price)) return OfferResult.NotEnoughCurrency;
            return OfferResult.Ok;
        }

        public static OfferResult TryBuyOffer(LimitedOfferDefinition offer)
        {
            var result = CheckOffer(offer);
            if (result != OfferResult.Ok) return result;
            if (!CurrencyService.TrySpend(offer.Price, offer.Id)) return OfferResult.NotEnoughCurrency;

            Data.LimitedOfferPurchases.SetLevel(offer.Id, Purchases(offer) + 1);
            LiveOpsRewards.GrantAll(offer.Rewards, offer.Id);
            Progress.Report(ProgressStatIds.MarketPurchase, 1, offer.Id);
            EventBus<LiveOpsChangedEvent>.Raise(new LiveOpsChangedEvent());
            return OfferResult.Ok;
        }
    }
}
