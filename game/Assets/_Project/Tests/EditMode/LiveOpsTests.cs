using System;
using System.Collections.Generic;
using NinjaVillage.Systems.LiveOps;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    public class LiveOpsTests
    {
        [Test]
        public void Dates_ParseAndWindow()
        {
            var start = LiveOpsRules.ParseDate("2026-09-20");
            Assert.IsTrue(start.HasValue);
            Assert.AreEqual(DateTimeKind.Utc, start.Value.Kind);
            Assert.IsNull(LiveOpsRules.ParseDate("20/09/2026"));
            Assert.IsNull(LiveOpsRules.ParseDate(null));

            Assert.IsTrue(LiveOpsRules.IsWithin(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc), start, 14), "start is inclusive");
            Assert.IsTrue(LiveOpsRules.IsWithin(new DateTime(2026, 10, 3, 23, 59, 0, DateTimeKind.Utc), start, 14));
            Assert.IsFalse(LiveOpsRules.IsWithin(new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Utc), start, 14), "end is exclusive");
            Assert.IsFalse(LiveOpsRules.IsWithin(new DateTime(2026, 9, 19, 23, 0, 0, DateTimeKind.Utc), start, 14));
            Assert.IsFalse(LiveOpsRules.IsWithin(DateTime.UtcNow, null, 14));
        }

        [Test]
        public void BattlePass_TiersAndProgress()
        {
            Assert.AreEqual(0, LiveOpsRules.TiersReached(299, 300, 30));
            Assert.AreEqual(1, LiveOpsRules.TiersReached(300, 300, 30));
            Assert.AreEqual(30, LiveOpsRules.TiersReached(999999, 300, 30), "capped at the tier count");
            Assert.AreEqual(0.5f, LiveOpsRules.TierProgress(450, 300, 30), 1e-5f);
            Assert.AreEqual(1f, LiveOpsRules.TierProgress(9000, 300, 30), 1e-5f, "complete pass shows a full bar");
        }

        [Test]
        public void BattlePass_ClaimRules()
        {
            var claimed = new List<int> { 0 };
            Assert.AreEqual(LiveOpsRules.ClaimResult.Ok, LiveOpsRules.CheckClaim(1, 3, 30, false, false, claimed));
            Assert.AreEqual(LiveOpsRules.ClaimResult.AlreadyClaimed, LiveOpsRules.CheckClaim(0, 3, 30, false, false, claimed));
            Assert.AreEqual(LiveOpsRules.ClaimResult.NotReached, LiveOpsRules.CheckClaim(3, 3, 30, false, false, claimed));
            Assert.AreEqual(LiveOpsRules.ClaimResult.PremiumLocked, LiveOpsRules.CheckClaim(1, 3, 30, true, false, new List<int>()));
            Assert.AreEqual(LiveOpsRules.ClaimResult.Ok, LiveOpsRules.CheckClaim(1, 3, 30, true, true, new List<int>()),
                "buying premium later unlocks already-reached premium tiers");
            Assert.AreEqual(LiveOpsRules.ClaimResult.InvalidTier, LiveOpsRules.CheckClaim(30, 30, 30, false, false, claimed));
        }

        [Test]
        public void RunXp_RewardsDepthWithCap()
        {
            Assert.AreEqual(30, LiveOpsRules.RunXp(0, false));
            Assert.AreEqual(30 + 50, LiveOpsRules.RunXp(10, false));
            Assert.AreEqual(30 + 150, LiveOpsRules.RunXp(500, false), "wave bonus capped");
            Assert.AreEqual(30 + 50 + 50, LiveOpsRules.RunXp(10, true));
        }

        [Test]
        public void LimitedOffers_PurchaseLimit()
        {
            Assert.IsTrue(LiveOpsRules.CanPurchase(0, 1));
            Assert.IsFalse(LiveOpsRules.CanPurchase(1, 1));
            Assert.IsTrue(LiveOpsRules.CanPurchase(99, 0), "0 = unlimited");
        }
    }
}
