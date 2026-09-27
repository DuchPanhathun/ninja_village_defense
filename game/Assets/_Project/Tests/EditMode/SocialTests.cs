using System;
using System.Collections.Generic;
using NinjaVillage.Systems.Backend;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Social;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    public class SocialTests
    {
        private static readonly DateTime T0 = new(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);
        private const int Today = 1000, Week = 142;

        [Test]
        public void RankKeys_SortThisWeekAboveOlderWeeks_ThenByLikes()
        {
            Assert.Greater(SocialRules.RankKey(Week, 5), SocialRules.RankKey(Week, 3));
            Assert.Greater(SocialRules.RankKey(Week, 1), SocialRules.RankKey(Week - 1, 999_999));
            Assert.Less(SocialRules.RankKey(Week, 999_999), SocialRules.RankKey(Week + 1, 0));
            Assert.AreEqual(SocialRules.RankKey(Week, 999_999), SocialRules.RankKey(Week, 5_000_000), "likes are capped inside the week");
        }

        [Test]
        public void LikesAreWeekly_GiftsAndWateringDaily()
        {
            var mark = new VisitMark { LikedWeek = Week, GiftDay = Today, WaterDay = Today - 1 };
            Assert.IsFalse(SocialRules.CanLike(mark, Week));
            Assert.IsTrue(SocialRules.CanLike(mark, Week + 1));
            Assert.IsFalse(SocialRules.CanGift(mark, Today));
            Assert.IsTrue(SocialRules.CanWater(mark, Today));
            Assert.IsTrue(SocialRules.CanLike(null, Week), "never visited");
        }

        [Test]
        public void TheOwnerReads_VisitorsLikesAndNewGiftsAndWaterings()
        {
            var visits = new List<VisitRecord>
            {
                new() { VisitorId = "me", VisitorName = "Me", VisitDay = Today, LikedWeek = Week, GiftDay = Today },
                new() { VisitorId = "a", VisitorName = "Aiko", VisitDay = Today, LikedWeek = Week, GiftDay = Today },
                new() { VisitorId = "b", VisitorName = "Kenji", VisitDay = Today, LikedWeek = Week - 1, WaterDay = Today, WaterTicks = 5 },
                new() { VisitorId = "c", VisitorName = "Jin", VisitDay = Today - 1, LikedWeek = Week, GiftDay = Today },
                new() { VisitorId = "d", VisitorName = "Old", VisitDay = Today - 20, GiftDay = Today - 20, WaterDay = Today - 3 },
            };
            var claimed = new Dictionary<string, int> { ["c"] = Today };
            var summary = SocialRules.Summarize(visits, "me", Today, Week,
                id => claimed.TryGetValue(id, out var d) ? d : 0, _ => 0);

            CollectionAssert.AreEqual(new[] { "Aiko", "Kenji" }, summary.VisitorsToday, "the owner isn't their own visitor");
            Assert.AreEqual(2, summary.LikesThisWeek, "Aiko and Jin this week; Kenji's like was last week");
            Assert.AreEqual(1, summary.NewGifts.Count, "Jin's gift was already claimed; old gifts expire");
            Assert.AreEqual("a", summary.NewGifts[0].VisitorId);
            Assert.AreEqual(1, summary.NewWaterings.Count);
            Assert.AreEqual("b", summary.NewWaterings[0].VisitorId);
            Assert.IsTrue(summary.HasNews);
        }

        [Test]
        public void ANeighboursWatering_SpeedsUpWhatWasGrowingThen()
        {
            long water = T0.AddMinutes(10).Ticks;
            var growing = new FarmPlotState { Plot = 0, PlantedTicks = T0.Ticks, ReadyTicks = T0.AddMinutes(110).Ticks };
            var plantedLater = new FarmPlotState { Plot = 1, PlantedTicks = T0.AddMinutes(20).Ticks, ReadyTicks = T0.AddMinutes(80).Ticks };
            var ripeAlready = new FarmPlotState { Plot = 2, PlantedTicks = T0.AddMinutes(-60).Ticks, ReadyTicks = T0.AddMinutes(5).Ticks };
            var wateredAlready = new FarmPlotState { Plot = 3, PlantedTicks = T0.Ticks, ReadyTicks = T0.AddMinutes(60).Ticks, Watered = true };
            var plots = new List<FarmPlotState> { growing, plantedLater, ripeAlready, wateredAlready };

            Assert.AreEqual(1, SocialRules.ApplyWatering(plots, water, T0.AddHours(1).Ticks));
            Assert.IsTrue(growing.Watered);
            Assert.AreEqual(T0.AddMinutes(10 + 70).Ticks, growing.ReadyTicks, TimeSpan.TicksPerSecond, "30% off the 100 minutes left at watering time");
            Assert.IsFalse(plantedLater.Watered);
            Assert.IsFalse(ripeAlready.Watered);
            Assert.AreEqual(T0.AddMinutes(60).Ticks, wateredAlready.ReadyTicks);
        }

        [Test]
        public void VisitorNames_ReadNaturally()
        {
            Assert.AreEqual("Aiko", SocialRules.Names(new[] { "Aiko" }));
            Assert.AreEqual("Aiko and Kenji", SocialRules.Names(new[] { "Aiko", "Kenji" }));
            Assert.AreEqual("Aiko, Kenji and 2 others", SocialRules.Names(new[] { "Aiko", "Kenji", "Jin", "Ume" }));
        }
    }
}
