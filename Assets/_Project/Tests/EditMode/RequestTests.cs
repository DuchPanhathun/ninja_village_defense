using System;
using System.Collections.Generic;
using System.IO;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Requests;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    public class RequestTests
    {
        private static readonly DateTime T0 = new(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);

        // ------------------------------------------------------------------ rules

        [Test]
        public void EveryVillager_HasAName()
        {
            var keys = new HashSet<string>();
            foreach (var (key, name) in RequestRules.Villagers)
            {
                Assert.IsTrue(keys.Add(key), $"{key} listed twice");
                Assert.IsFalse(string.IsNullOrEmpty(name));
                Assert.AreEqual(name, RequestRules.VillagerName(key));
            }
            Assert.AreEqual("A villager", RequestRules.VillagerName("npc_nobody"));
        }

        [Test]
        public void Requests_OnlyComeUpOnceTheVillageCanDoThem()
        {
            Assert.IsFalse(RequestRules.IsEligible(RequestKind.Deliver, 2, 0, 1, 0, 1), "carrots need castle 2");
            Assert.IsTrue(RequestRules.IsEligible(RequestKind.Deliver, 2, 0, 2, 0, 1));
            Assert.IsFalse(RequestRules.IsEligible(RequestKind.Deliver, 1, 1, 3, 0, 1), "meals need a kitchen");
            Assert.IsFalse(RequestRules.IsEligible(RequestKind.Chapter, 1, 0, 5, 0, 0), "every chapter already cleared");
            Assert.IsTrue(RequestRules.IsEligible(RequestKind.Chapter, 1, 0, 1, 0, 2));
        }

        [Test]
        public void CoinRewards_GrowWithTheCastle()
        {
            Assert.AreEqual(100, RequestRules.ScaledCoins(100, 1));
            Assert.AreEqual(140, RequestRules.ScaledCoins(100, 3));
            Assert.AreEqual(95, RequestRules.ScaledCoins(80, 2));
            Assert.AreEqual(0, RequestRules.ScaledCoins(0, 5));
        }

        [Test]
        public void Progress_ByKind()
        {
            Assert.AreEqual(3, RequestRules.Progress(RequestKind.Deliver, 4, 0, 3, 0, 0));
            Assert.IsFalse(RequestRules.IsComplete(RequestKind.Deliver, 4, 0, 3, 0, 0));
            Assert.IsTrue(RequestRules.IsComplete(RequestKind.Deliver, 4, 0, 9, 0, 0), "extra in stock is fine");
            Assert.IsTrue(RequestRules.IsComplete(RequestKind.Stat, 5, 5, 0, 0, 0));
            Assert.IsFalse(RequestRules.IsComplete(RequestKind.Chapter, 1, 0, 0, 2, 3));
            Assert.IsTrue(RequestRules.IsComplete(RequestKind.Chapter, 1, 0, 0, 3, 3));
            Assert.AreEqual(1, RequestRules.ShownTarget(RequestKind.Chapter, 1));
        }

        [Test]
        public void EachRequest_ComesFromADifferentVillager_PreferablyOneWhoLikesAsking()
        {
            var everyone = new List<string> { "a", "b", "c", "d" };
            var taken = new HashSet<string>();
            var rng = new Random(7);
            Assert.AreEqual("b", RequestRules.PickVillager(new[] { "b" }, everyone, taken, rng));
            taken.Add("b");
            string other = RequestRules.PickVillager(new[] { "b" }, everyone, taken, rng);
            Assert.AreNotEqual("b", other, "b already asked today");
            taken.Add(other);
            Assert.IsFalse(taken.Contains(RequestRules.PickVillager(null, everyone, taken, rng)));
        }

        [Test]
        public void Titles_ReadNaturally()
        {
            Assert.AreEqual("Bring 4 Carrots", RequestRules.Title("Bring {0} {1}", 4, "Carrot"));
            Assert.AreEqual("Bring 3 Radishes", RequestRules.Title("Bring {0} {1}", 3, "Radish"));
            Assert.AreEqual("Bring 6 Rice", RequestRules.Title("Bring {0} {1}", 6, "Rice"));
            Assert.AreEqual("Bring 2 Onigiri", RequestRules.Title("Bring {0} {1}", 2, "Onigiri"));
            Assert.AreEqual("Bring a Noodle Bowl", RequestRules.Title("Bring a {1}", 1, "Noodle Bowl"));
            Assert.AreEqual("Clear Chapter 3", RequestRules.Title("Clear Chapter {0}", 3, null));
        }

        // ------------------------------------------------------------------ service (temp save, fixed clock)

        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "nv_requests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            SaveSystem.OverrideDirectory = _dir;
            SaveService.ResetAll();
            GameClock.OverrideUtcNow = () => T0;
        }

        [TearDown]
        public void TearDown()
        {
            GameClock.OverrideUtcNow = null;
            SaveSystem.OverrideDirectory = null;
            SaveService.Load();
            try { Directory.Delete(_dir, true); } catch { /* best effort */ }
        }

        [Test]
        public void ThreeRequestsArePostedEachDay_FromThreeVillagers_AndReplacedTheNextDay()
        {
            Assume.That(RequestService.Catalog != null && RequestService.Catalog.All.Count > 0, "request catalog missing — run the content generator");
            var today = new List<VillagerRequestState>(RequestService.Active);
            Assert.AreEqual(RequestRules.ActiveCount, today.Count);
            var ids = new HashSet<string>();
            var villagers = new HashSet<string>();
            foreach (var state in today)
            {
                Assert.IsTrue(ids.Add(state.Id), "no repeats");
                Assert.IsTrue(villagers.Add(state.VillagerKey), "one request per villager");
                var request = RequestService.Get(state.Id);
                Assert.LessOrEqual(request.RequiredCastleLevel, 1, $"{state.Id} needs a bigger castle");
                Assert.AreEqual(0, request.RequiredKitchenLevel, $"{state.Id} needs a kitchen");
            }
            Assert.IsFalse(RequestService.EnsureRolled(), "same day, same requests");

            GameClock.OverrideUtcNow = () => T0.AddDays(1);
            Assert.IsTrue(RequestService.EnsureRolled(), "a new day posts new requests");
            Assert.AreEqual(RequestRules.ActiveCount, RequestService.Active.Count);
        }

        [Test]
        public void Delivering_UsesTheGoods_CountsStats_AndPaysOnce()
        {
            var rice = RequestService.Get("bring_rice");
            Assume.That(rice != null, "request catalog missing — run the content generator");
            var data = SaveService.Data.Requests;
            data.Day = GameClock.Today;
            data.Active.Clear();
            data.Active.Add(new VillagerRequestState { Id = "bring_rice", VillagerKey = "npc_villager", Target = 6 });
            data.Active.Add(new VillagerRequestState { Id = "defeat_enemies", VillagerKey = "npc_master", Target = 150 });
            data.Active.Add(new VillagerRequestState { Id = "clear_chapter", VillagerKey = "npc_oldman", Target = 1, Chapter = 1 });
            var (deliver, stat, chapter) = (data.Active[0], data.Active[1], data.Active[2]);

            // Bring 6 rice
            Assert.IsFalse(RequestService.CanDeliver(deliver));
            GoodsService.Add("rice", 7);
            Assert.AreEqual(1, RequestService.DeliverableCount());
            int coins = SaveService.Data.Wallet.Get(CurrencyType.Coins);
            Assert.IsTrue(RequestService.Deliver(deliver, out var reward));
            Assert.AreEqual(1, GoodsService.Count("rice"), "six handed over");
            Assert.AreEqual(coins + reward.Coins, SaveService.Data.Wallet.Get(CurrencyType.Coins));
            Assert.IsFalse(RequestService.Deliver(deliver, out _), "only once");

            // Defeat 150 enemies (counted from the progress feed)
            RequestService.Record(Core.Events.ProgressStatIds.EnemiesKilled, 100, false);
            Assert.AreEqual(100, RequestService.Progress(stat));
            RequestService.Record(Core.Events.ProgressStatIds.EnemiesKilled, 80, false);
            Assert.AreEqual(150, RequestService.Progress(stat), "capped at the target");
            Assert.IsTrue(RequestService.Deliver(stat, out _));

            // Clear chapter 1 → gems and a decoration to place for free
            Assert.IsFalse(RequestService.CanDeliver(chapter));
            SaveService.Data.Chapters.HighestCleared = 1;
            int gems = SaveService.Data.Wallet.Get(CurrencyType.Gems);
            Assert.IsTrue(RequestService.Deliver(chapter, out var chapterReward));
            Assert.Greater(chapterReward.Gems, 0);
            Assert.AreEqual(gems + chapterReward.Gems, SaveService.Data.Wallet.Get(CurrencyType.Gems));
            Assert.IsNotNull(chapterReward.Decoration);
            Assert.AreEqual(1, DecorationService.GiftCount(chapterReward.Decoration));
            Assert.AreEqual(0, RequestService.DeliverableCount());
        }
    }
}
