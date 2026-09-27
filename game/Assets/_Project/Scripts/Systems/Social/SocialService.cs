using System.Threading.Tasks;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Backend;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using UnityEngine;

namespace NinjaVillage.Systems.Social
{
    /// <summary>Raised when the owner's visitor news has been read (and applied).</summary>
    public readonly struct VisitorsUpdatedEvent : IGameEvent
    {
        public readonly VisitSummary Summary;
        public VisitorsUpdatedEvent(VisitSummary summary) => Summary = summary;
    }

    /// <summary>
    /// Social (EPIC 24 Phase 6), on top of village visiting. As a visitor: every visit leaves a record on that
    /// village (<c>villages/{owner}/visits/{me}</c>), and Like (weekly), Gift (daily) and Water crops (daily, pays
    /// <see cref="SocialRules.HelperCoins"/>) add to it. As the owner: <see cref="RefreshAsync"/> reads the records,
    /// turns new gifts into coins, applies new waterings to the farm, counts this week's likes and republishes them
    /// for the Best Village ranking, and once a week gives last week's top villages a trophy decoration.
    /// </summary>
    public static class SocialService
    {
        public const float RefreshCooldownSeconds = 60f;

        private static SocialSaveData Data => SaveService.Data.Social;
        private static IBackendProvider Provider => BackendService.Provider;

        public static string MyId => Provider?.CurrentUser?.UserId ?? SaveService.Data.Profile.PlayerId;
        public static int LikesThisWeek => Data.LikesWeek == GameClock.ThisWeek ? Data.Likes : 0;
        public static VisitSummary LastSummary { get; private set; }

        // ------------------------------------------------------------------ visiting someone

        private static VisitMark Find(string villageId)
        {
            foreach (var mark in Data.MyVisits)
                if (mark != null && mark.VillageId == villageId) return mark;
            return null;
        }

        public static bool CanLike(string villageId) => SocialRules.CanLike(Find(villageId), GameClock.ThisWeek);
        public static bool CanGift(string villageId) => SocialRules.CanGift(Find(villageId), GameClock.Today);
        public static bool CanWater(string villageId) => SocialRules.CanWater(Find(villageId), GameClock.Today);

        /// <summary>Signs the visitors' book (counts towards "N ninjas visited today").</summary>
        public static Task<bool> RecordVisitAsync(string villageId) => ChangeAsync(villageId, mark => mark.VisitDay = GameClock.Today);

        public static Task<bool> LikeAsync(string villageId) =>
            CanLike(villageId) ? ChangeAsync(villageId, mark => mark.LikedWeek = GameClock.ThisWeek) : Task.FromResult(false);

        public static Task<bool> GiftAsync(string villageId) =>
            CanGift(villageId) ? ChangeAsync(villageId, mark => mark.GiftDay = GameClock.Today) : Task.FromResult(false);

        /// <summary>Waters their crops (they grow faster from now) and pays the helper a few coins.</summary>
        public static async Task<bool> WaterAsync(string villageId)
        {
            if (!CanWater(villageId)) return false;
            bool ok = await ChangeAsync(villageId, mark =>
            {
                mark.WaterDay = GameClock.Today;
                mark.WaterTicks = GameClock.UtcNow.Ticks;
            });
            if (ok) CurrencyService.Grant(CurrencyType.Coins, SocialRules.HelperCoins, "helped_neighbour");
            return ok;
        }

        /// <summary>Applies <paramref name="change"/> to our mark on that village and writes the record; undone if the write fails.</summary>
        private static async Task<bool> ChangeAsync(string villageId, System.Action<VisitMark> change)
        {
            if (Provider == null || string.IsNullOrEmpty(villageId) || villageId == MyId) return false;
            var mark = Data.Mark(villageId);
            var before = new VisitMark
            {
                VillageId = mark.VillageId, VisitDay = mark.VisitDay, LikedWeek = mark.LikedWeek,
                GiftDay = mark.GiftDay, WaterDay = mark.WaterDay, WaterTicks = mark.WaterTicks,
            };
            change(mark);
            mark.VisitDay = GameClock.Today;
            bool ok = await Provider.WriteVisitAsync(new VisitRecord
            {
                VillageId = villageId, VisitorId = MyId, VisitorName = SaveService.Data.Profile.DisplayName,
                VisitDay = mark.VisitDay, LikedWeek = mark.LikedWeek, GiftDay = mark.GiftDay,
                WaterDay = mark.WaterDay, WaterTicks = mark.WaterTicks,
            });
            if (!ok)
            {
                mark.VisitDay = before.VisitDay;
                mark.LikedWeek = before.LikedWeek;
                mark.GiftDay = before.GiftDay;
                mark.WaterDay = before.WaterDay;
                mark.WaterTicks = before.WaterTicks;
            }
            SaveService.MarkDirty();
            return ok;
        }

        // ------------------------------------------------------------------ your own village

        private static bool _refreshing;
        private static float _lastRefresh = float.MinValue;

        /// <summary>
        /// Reads the visitors' records and applies the news (gift coins, watered crops, likes, a weekly trophy).
        /// Throttled to once a minute unless <paramref name="force"/>d. Raises <see cref="VisitorsUpdatedEvent"/>.
        /// </summary>
        public static async Task<VisitSummary> RefreshAsync(bool force = false)
        {
            var provider = Provider;
            if (provider == null || _refreshing) return LastSummary;
            if (!force && Time.unscaledTime - _lastRefresh < RefreshCooldownSeconds) return LastSummary;
            _refreshing = true;
            _lastRefresh = Time.unscaledTime;
            try
            {
                string me = MyId;
                var visits = await provider.ListVisitsAsync(me);
                var data = Data;
                int today = GameClock.Today, week = GameClock.ThisWeek;
                var summary = SocialRules.Summarize(visits, me, today, week,
                    id => data.GiftsClaimed.GetLevel(id), id => data.WateringsApplied.GetLevel(id)); // 0 = never (day indices are positive)

                foreach (var gift in summary.NewGifts)
                {
                    data.GiftsClaimed.SetLevel(gift.VisitorId, gift.GiftDay);
                    summary.GiftCoins += SocialRules.GiftCoins;
                }
                if (summary.GiftCoins > 0) CurrencyService.Grant(CurrencyType.Coins, summary.GiftCoins, "neighbour_gifts");

                long now = GameClock.UtcNow.Ticks;
                foreach (var watering in summary.NewWaterings)
                {
                    data.WateringsApplied.SetLevel(watering.VisitorId, watering.WaterDay);
                    summary.CropsWatered += SocialRules.ApplyWatering(SaveService.Data.Farm.Plots, watering.WaterTicks, now);
                }

                bool likesChanged = data.Likes != summary.LikesThisWeek || data.LikesWeek != week;
                data.Likes = summary.LikesThisWeek;
                data.LikesWeek = week;

                await CheckTrophyAsync(provider, me, week, summary);
                SaveService.MarkDirty();
                if (likesChanged) _ = BackendService.PublishVillageNowAsync();

                LastSummary = summary;
                EventBus<VisitorsUpdatedEvent>.Raise(new VisitorsUpdatedEvent(summary));
                return summary;
            }
            finally
            {
                _refreshing = false;
            }
        }

        /// <summary>Once a week: if this village made last week's top <see cref="SocialRules.TrophyPlaces"/>, a trophy decoration.</summary>
        private static async Task CheckTrophyAsync(IBackendProvider provider, string me, int week, VisitSummary summary)
        {
            var data = Data;
            if (data.TrophyCheckedWeek >= week) return;
            var top = await provider.ListTopVillagesAsync(week - 1, SocialRules.TrophyPlaces);
            if (!provider.IsOnline && provider is not OfflineBackendProvider) return; // try again when connected
            data.TrophyCheckedWeek = week;
            for (int i = 0; i < top.Count; i++)
            {
                if (top[i] == null || top[i].UserId != me) continue;
                summary.TrophyRank = i + 1;
                data.TrophiesWon++;
                DecorationService.Gift(DecorationService.Get(SocialRules.TrophyDecorationId), 1);
                break;
            }
        }

        /// <summary>The line shown when you come home: "3 ninjas visited today · 2 likes · a gift (+30 coins) · crops watered".</summary>
        public static string Describe(VisitSummary summary)
        {
            if (summary == null) return string.Empty;
            var parts = new System.Collections.Generic.List<string>();
            int visitors = summary.VisitorsToday.Count;
            if (visitors > 0) parts.Add($"{visitors} ninja{(visitors == 1 ? "" : "s")} visited today");
            if (summary.GiftCoins > 0)
                parts.Add($"{summary.NewGifts.Count} gift{(summary.NewGifts.Count == 1 ? "" : "s")} (+{summary.GiftCoins} coins)");
            if (summary.CropsWatered > 0) parts.Add($"{SocialRules.Names(summary.NewWaterings.ConvertAll(v => v.VisitorName))} watered your crops");
            if (summary.TrophyRank > 0) parts.Add($"#{summary.TrophyRank} Best Village last week: a trophy is in your Decorate gifts!");
            return string.Join(" · ", parts);
        }
    }
}
