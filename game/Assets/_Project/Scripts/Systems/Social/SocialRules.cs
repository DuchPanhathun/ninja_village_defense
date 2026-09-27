using System;
using System.Collections.Generic;
using NinjaVillage.Systems.Backend;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Social
{
    /// <summary>What the visitors' records say, from the village owner's side.</summary>
    public sealed class VisitSummary
    {
        public readonly List<string> VisitorsToday = new();
        public int LikesThisWeek;
        public readonly List<VisitRecord> NewGifts = new();
        public readonly List<VisitRecord> NewWaterings = new();

        // Filled in by the service when it applies the news.
        public int GiftCoins;
        public int CropsWatered;
        /// <summary>Last week's Best Village place when it earned a trophy (1-3), else 0.</summary>
        public int TrophyRank;

        public bool HasNews => NewGifts.Count > 0 || NewWaterings.Count > 0 || TrophyRank > 0;
    }

    /// <summary>
    /// Social rules as pure functions (EPIC 24 Phase 6), kept free of the backend and the save so EditMode tests
    /// pin them down. A visitor can like a village once a week, leave a gift once a day and water its crops once a
    /// day (and earns a few coins for helping). The owner counts likes by distinct visitors per week, turns each new
    /// gift into coins once, and applies each new watering to whatever was growing at that moment. Villages rank by
    /// likes within a week; the top three of a week get a trophy.
    /// </summary>
    public static class SocialRules
    {
        public const int GiftCoins = 30;
        public const int HelperCoins = 10;
        public const int TrophyPlaces = 3;
        public const string TrophyDecorationId = "best_village_trophy";
        private const long WeekFactor = 1_000_000;

        /// <summary>Sort key for the weekly ranking: every village of a week sits above every older one, by likes.</summary>
        public static long RankKey(int week, int likes) => week * WeekFactor + Math.Clamp(likes, 0, (int)WeekFactor - 1);

        public static bool CanLike(VisitMark mark, int week) => mark == null || mark.LikedWeek != week;
        public static bool CanGift(VisitMark mark, int day) => mark == null || mark.GiftDay != day;
        public static bool CanWater(VisitMark mark, int day) => mark == null || mark.WaterDay != day;

        /// <summary>
        /// Reads the visitors' records: who came today, this week's likes, and gifts / waterings not handled yet
        /// (<paramref name="giftClaimed"/> / <paramref name="waterApplied"/> give the last day handled per visitor).
        /// Gifts older than a week and waterings older than yesterday are ignored; the owner's own id is skipped.
        /// </summary>
        public static VisitSummary Summarize(IEnumerable<VisitRecord> visits, string ownerId, int today, int week,
            Func<string, int> giftClaimed, Func<string, int> waterApplied)
        {
            var summary = new VisitSummary();
            if (visits == null) return summary;
            foreach (var visit in visits)
            {
                if (visit == null || string.IsNullOrEmpty(visit.VisitorId) || visit.VisitorId == ownerId) continue;
                string name = string.IsNullOrEmpty(visit.VisitorName) ? "A ninja" : visit.VisitorName;
                if (visit.VisitDay == today) summary.VisitorsToday.Add(name);
                if (visit.LikedWeek == week) summary.LikesThisWeek++;
                if (visit.GiftDay > giftClaimed(visit.VisitorId) && visit.GiftDay >= today - 6 && visit.GiftDay <= today) summary.NewGifts.Add(visit);
                if (visit.WaterDay > waterApplied(visit.VisitorId) && visit.WaterDay >= today - 1 && visit.WaterDay <= today) summary.NewWaterings.Add(visit);
            }
            return summary;
        }

        /// <summary>
        /// A neighbour watered the farm at <paramref name="waterTicks"/>: every crop that was growing then and not
        /// watered yet gets the watering speed-up from that moment. Returns how many crops it helped.
        /// </summary>
        public static int ApplyWatering(List<FarmPlotState> plots, long waterTicks, long nowTicks)
        {
            if (plots == null) return 0;
            long at = Math.Min(waterTicks <= 0 ? nowTicks : waterTicks, nowTicks);
            var when = new DateTime(at, DateTimeKind.Utc);
            int watered = 0;
            foreach (var plot in plots)
            {
                if (plot == null || plot.Watered || plot.PlantedTicks > at || plot.ReadyTicks <= at) continue;
                plot.ReadyTicks = FarmRules.WaterReadyTicks(when, plot.ReadyTicks);
                plot.Watered = true;
                watered++;
            }
            return watered;
        }

        /// <summary>"Aiko", "Aiko and Kenji", "Aiko, Kenji and 2 others" for the visitors line.</summary>
        public static string Names(IReadOnlyList<string> names)
        {
            if (names == null || names.Count == 0) return string.Empty;
            if (names.Count == 1) return names[0];
            if (names.Count == 2) return $"{names[0]} and {names[1]}";
            int others = names.Count - 2;
            return $"{names[0]}, {names[1]} and {others} other{(others == 1 ? "" : "s")}";
        }
    }
}
