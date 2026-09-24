using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Daily;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    public class DailyTests
    {
        [Test]
        public void Login_OneClaimPerDay_ClockBackwardsBlocks()
        {
            Assert.IsTrue(DailyRules.CanClaimLogin(-1, 100), "never claimed");
            Assert.IsFalse(DailyRules.CanClaimLogin(100, 100), "already claimed today");
            Assert.IsTrue(DailyRules.CanClaimLogin(100, 101));
            Assert.IsFalse(DailyRules.CanClaimLogin(100, 99), "device clock moved backwards");
        }

        [Test]
        public void Login_StreakResetsAfterMissedDay_CalendarNeverResets()
        {
            Assert.AreEqual(1, DailyRules.NextStreak(-1, 0, 10));
            Assert.AreEqual(4, DailyRules.NextStreak(9, 3, 10));
            Assert.AreEqual(1, DailyRules.NextStreak(7, 3, 10), "missed a day");

            Assert.AreEqual(0, DailyRules.CalendarIndex(0, 7));
            Assert.AreEqual(6, DailyRules.CalendarIndex(6, 7));
            Assert.AreEqual(0, DailyRules.CalendarIndex(7, 7), "loops after day 7");
        }

        [Test]
        public void QuestRoll_DeterministicDistinctAndWeighted()
        {
            var pool = new List<(string, float)> { ("a", 1f), ("b", 1f), ("c", 1f), ("d", 1f), ("never", 0f) };
            var first = new List<string>();
            var second = new List<string>();
            DailyRules.RollQuests(pool, 3, 42, first);
            DailyRules.RollQuests(pool, 3, 42, second);

            CollectionAssert.AreEqual(first, second);
            Assert.AreEqual(3, first.Count);
            CollectionAssert.AllItemsAreUnique(first);
            CollectionAssert.DoesNotContain(first, "never", "zero-weight quests are never rolled");

            // Catalog order must not matter.
            var reversed = new List<(string, float)>(pool);
            reversed.Reverse();
            var third = new List<string>();
            DailyRules.RollQuests(reversed, 3, 42, third);
            CollectionAssert.AreEqual(first, third);
        }

        [Test]
        public void Progress_SumOrMax()
        {
            Assert.AreEqual(15, DailyRules.ApplyProgress(10, 5, isMax: false));
            Assert.AreEqual(10, DailyRules.ApplyProgress(10, 5, isMax: true));
            Assert.AreEqual(12, DailyRules.ApplyProgress(10, 12, isMax: true));
            Assert.AreEqual(int.MaxValue, DailyRules.ApplyProgress(int.MaxValue - 1, 5, isMax: false), "no overflow");
            Assert.IsTrue(DailyRules.IsMaxStat(ProgressStatIds.WaveReached));
            Assert.IsFalse(DailyRules.IsMaxStat(ProgressStatIds.EnemiesKilled));
        }

        [Test]
        public void CombatStats_OnlyFromCombatEvents()
        {
            Assert.IsTrue(DailyRules.IsCombatDerivedStat(ProgressStatIds.EnemiesKilled));
            Assert.IsFalse(DailyRules.IsCombatDerivedStat(ProgressStatIds.BuildingUpgraded));
        }

        [Test]
        public void Achievements_TiersReachedAndClaimable()
        {
            var targets = new[] { 10, 100, 1000 };
            Assert.AreEqual(0, DailyRules.ReachedTiers(9, targets));
            Assert.AreEqual(2, DailyRules.ReachedTiers(150, targets));
            Assert.AreEqual(3, DailyRules.ReachedTiers(5000, targets));
            Assert.AreEqual(1, DailyRules.ClaimableTiers(150, 1, targets));
            Assert.AreEqual(0, DailyRules.ClaimableTiers(150, 2, targets));
        }
    }
}
