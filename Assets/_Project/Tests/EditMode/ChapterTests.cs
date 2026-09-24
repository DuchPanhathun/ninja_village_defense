using NinjaVillage.Systems.Chapters;
using NinjaVillage.Systems.Save;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    public class ChapterTests
    {
        [Test]
        public void Chapters_UnlockInOrder()
        {
            Assert.IsTrue(ChapterRules.IsUnlocked(1, 0), "chapter 1 is always open");
            Assert.IsFalse(ChapterRules.IsUnlocked(2, 0));
            Assert.IsTrue(ChapterRules.IsUnlocked(2, 1));
            Assert.IsFalse(ChapterRules.IsUnlocked(0, 5));
            Assert.IsTrue(ChapterRules.IsCleared(1, 1));
            Assert.IsFalse(ChapterRules.IsCleared(2, 1));
        }

        [Test]
        public void FirstClear_PaysOnce_AndUnlocksTheNextChapter()
        {
            var data = new ChapterSaveData();
            var first = ChapterRules.Record(data, "chapter_01", 1, 10, victory: true, waveReached: 10, clearCoins: 400, clearGems: 20);
            Assert.IsTrue(first.FirstClear);
            Assert.AreEqual(400, first.RewardCoins);
            Assert.AreEqual(20, first.RewardGems);
            Assert.AreEqual(1, data.HighestCleared);
            Assert.IsTrue(ChapterRules.IsUnlocked(2, data.HighestCleared));

            var replay = ChapterRules.Record(data, "chapter_01", 1, 10, victory: true, waveReached: 10, clearCoins: 400, clearGems: 20);
            Assert.IsFalse(replay.FirstClear, "replays don't pay the clear reward again");
            Assert.AreEqual(0, replay.RewardCoins);
            Assert.AreEqual(1, data.HighestCleared);
        }

        [Test]
        public void Defeat_RecordsBestWave_WithoutClearing()
        {
            var data = new ChapterSaveData();
            var run = ChapterRules.Record(data, "chapter_01", 1, 10, victory: false, waveReached: 6, clearCoins: 400, clearGems: 20);
            Assert.IsTrue(run.NewBestWave);
            Assert.IsFalse(run.FirstClear);
            Assert.AreEqual(0, data.HighestCleared);
            Assert.AreEqual(6, data.BestWaves.GetLevel("chapter_01"));

            var worse = ChapterRules.Record(data, "chapter_01", 1, 10, victory: false, waveReached: 3, clearCoins: 400, clearGems: 20);
            Assert.IsFalse(worse.NewBestWave);
            Assert.AreEqual(6, data.BestWaves.GetLevel("chapter_01"));
        }

        [Test]
        public void LockedChapter_CannotBeClearedOutOfOrder()
        {
            var data = new ChapterSaveData();
            var run = ChapterRules.Record(data, "chapter_03", 3, 10, victory: true, waveReached: 10, clearCoins: 800, clearGems: 30);
            Assert.IsFalse(run.FirstClear);
            Assert.AreEqual(0, data.HighestCleared);
        }

        [Test]
        public void OldSaves_GetAChapterSection()
        {
            var save = new SaveData { Chapters = null };
            save.Migrate();
            Assert.IsNotNull(save.Chapters);
            Assert.IsNotNull(save.Chapters.BestWaves);
            Assert.AreEqual(0, save.Chapters.HighestCleared);
        }
    }
}
