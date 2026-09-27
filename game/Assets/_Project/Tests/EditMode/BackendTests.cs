using System.Collections.Generic;
using NinjaVillage.Systems.Backend;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    public class BackendTests
    {
        private static SaveProgressSummary Progress(int runs, int wave = 0, int buildings = 1, long coins = 0) =>
            new() { TotalRuns = runs, HighestWave = wave, TotalBuildingLevels = buildings, Coins = coins };

        private static CloudSaveSnapshot Cloud(long ticks, SaveProgressSummary progress) =>
            new() { Json = "{}", LastSavedUtcTicks = ticks, Progress = progress };

        [Test]
        public void CloudSave_NoCloudCopy_UploadsLocal()
        {
            Assert.AreEqual(CloudSaveDecision.UseLocal, BackendRules.Decide(100, Progress(3), null));
            Assert.AreEqual(CloudSaveDecision.UseLocal, BackendRules.Decide(100, Progress(3), new CloudSaveSnapshot()));
        }

        [Test]
        public void CloudSave_SameTimestamp_InSync()
        {
            Assert.AreEqual(CloudSaveDecision.InSync, BackendRules.Decide(100, Progress(3), Cloud(100, Progress(3))));
        }

        [Test]
        public void CloudSave_FreshInstall_TakesCloudProgress()
        {
            // New phone: the fresh local save is "newer" but must not overwrite real progress.
            Assert.AreEqual(CloudSaveDecision.UseCloud, BackendRules.Decide(999, Progress(0), Cloud(100, Progress(12, 9))));
            Assert.AreEqual(CloudSaveDecision.UseLocal, BackendRules.Decide(100, Progress(5, 4), Cloud(999, Progress(0))));
        }

        [Test]
        public void CloudSave_NewerWins_UnlessOlderHasMoreProgress()
        {
            Assert.AreEqual(CloudSaveDecision.UseCloud, BackendRules.Decide(100, Progress(5, 4), Cloud(200, Progress(6, 4))));
            Assert.AreEqual(CloudSaveDecision.UseLocal, BackendRules.Decide(200, Progress(6, 4), Cloud(100, Progress(5, 4))));
            // Cloud is newer but the local copy played many more runs offline → keep local.
            Assert.AreEqual(CloudSaveDecision.UseLocal, BackendRules.Decide(100, Progress(40, 20), Cloud(200, Progress(10, 8))));
        }

        [Test]
        public void AnalyticsNames_SanitizedToFirebaseRules()
        {
            Assert.AreEqual("stat_building_upgraded", BackendRules.SanitizeName("stat_building_upgraded"));
            Assert.AreEqual("run_end", BackendRules.SanitizeName("Run-End"));
            Assert.AreEqual("e_1st_blood", BackendRules.SanitizeName("1st blood"));
            Assert.AreEqual("x_firebase_thing", BackendRules.SanitizeName("firebase_thing"));
            Assert.AreEqual(BackendRules.MaxEventNameLength, BackendRules.SanitizeName(new string('a', 100)).Length);
            Assert.AreEqual("unnamed", BackendRules.SanitizeName(""));
        }

        [Test]
        public void AnalyticsParams_CoercedCappedAndTruncated()
        {
            var raw = new List<AnalyticsParam>();
            for (int i = 0; i < 30; i++) raw.Add(new AnalyticsParam($"P{i}", i));
            raw[0] = new AnalyticsParam("text", new string('x', 500));
            raw[1] = new AnalyticsParam("flag", true);
            raw[2] = new AnalyticsParam("ratio", 0.5f);

            var clean = BackendRules.SanitizeParams(raw);
            Assert.AreEqual(BackendRules.MaxParams, clean.Count);
            Assert.AreEqual(BackendRules.MaxStringValueLength, ((string)clean[0].Value).Length);
            Assert.AreEqual(1L, clean[1].Value);
            Assert.IsInstanceOf<double>(clean[2].Value);
            Assert.IsInstanceOf<long>(clean[3].Value);
            Assert.AreEqual("p3", clean[3].Name);
        }

        [Test]
        public void Leaderboard_NewBestByWaveThenKills()
        {
            Assert.IsTrue(BackendRules.IsNewBest(5, 100, 6, 0));
            Assert.IsTrue(BackendRules.IsNewBest(5, 100, 5, 101));
            Assert.IsFalse(BackendRules.IsNewBest(5, 100, 5, 100));
            Assert.IsFalse(BackendRules.IsNewBest(5, 100, 4, 999));
        }

        [Test]
        public void Uploads_ThrottledUnlessForced()
        {
            Assert.IsFalse(BackendRules.ShouldUpload(false, 100f, 0f, 30f, force: true), "nothing pending");
            Assert.IsFalse(BackendRules.ShouldUpload(true, 10f, 0f, 30f, force: false));
            Assert.IsTrue(BackendRules.ShouldUpload(true, 31f, 0f, 30f, force: false));
            Assert.IsTrue(BackendRules.ShouldUpload(true, 10f, 0f, 30f, force: true), "app going to background");
        }
    }
}
