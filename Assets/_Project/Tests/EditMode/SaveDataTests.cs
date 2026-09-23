using System;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Save;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    public class SaveDataTests
    {
        [Test]
        public void RoundTrip_PreservesSections()
        {
            var data = new SaveData();
            data.Migrate();
            data.Wallet.Coins = 123;
            data.Heroes.Owned.SetLevel("samurai", 4);
            data.Heroes.SelectedHeroId = "samurai";
            data.Inventory.AddEquipment("iron_ring", 2);
            data.Village.Buildings.SetLevel(BuildingIds.Dojo, 3);

            var copy = SaveSystem.FromJson(SaveSystem.ToJson(data));

            Assert.AreEqual(123, copy.Wallet.Coins);
            Assert.AreEqual(4, copy.Heroes.GetLevel("samurai"));
            Assert.AreEqual("samurai", copy.Heroes.SelectedHeroId);
            Assert.AreEqual(2, copy.Inventory.GetEquipmentCount("iron_ring"));
            Assert.AreEqual(3, copy.Village.GetBuildingLevel(BuildingIds.Dojo));
            Assert.AreEqual(data.Profile.PlayerId, copy.Profile.PlayerId);
        }

        [Test]
        public void Migrate_FromV1_MovesRootStatsIntoProfile()
        {
            const string v1Json = "{\"Wallet\":{\"Coins\":50,\"Gems\":2},\"HighestWaveReached\":7,\"TotalRunsCompleted\":3}";

            var data = SaveSystem.FromJson(v1Json);

            Assert.AreEqual(SaveData.CurrentVersion, data.Version);
            Assert.AreEqual(50, data.Wallet.Coins);
            Assert.AreEqual(7, data.Profile.HighestWaveReached);
            Assert.AreEqual(3, data.Profile.TotalRuns);
            Assert.IsFalse(string.IsNullOrEmpty(data.Profile.PlayerId));
            Assert.IsNotNull(data.Store);
            Assert.IsNotNull(data.LiveOps.BattlePass);
        }

        [Test]
        public void FromJson_Garbage_ReturnsNull()
        {
            Assert.IsNull(SaveSystem.FromJson("not json"));
            Assert.IsNull(SaveSystem.FromJson(""));
        }

        [Test]
        public void Profile_RecordRun_CapsHistoryAndTracksBests()
        {
            var profile = new ProfileSaveData();
            profile.EnsureInitialized();

            for (int i = 0; i < ProfileSaveData.MaxRunHistory + 5; i++)
                profile.RecordRun(new RunRecord { WaveReached = i, Kills = 2, CoinsEarned = 10, Victory = i % 2 == 0 });

            Assert.AreEqual(ProfileSaveData.MaxRunHistory, profile.RecentRuns.Count);
            Assert.AreEqual(ProfileSaveData.MaxRunHistory + 5, profile.TotalRuns);
            Assert.AreEqual(ProfileSaveData.MaxRunHistory + 4, profile.HighestWaveReached);
            Assert.AreEqual((ProfileSaveData.MaxRunHistory + 5) * 2, profile.TotalKills);
            Assert.AreEqual(ProfileSaveData.MaxRunHistory + 4, profile.RecentRuns[0].WaveReached, "newest first");
        }

        [Test]
        public void Inventory_RemoveLastEquipment_Unequips()
        {
            var inv = new InventorySaveData();
            inv.AddEquipment("cloak");
            inv.EquippedEquipmentIds.Add("cloak");

            Assert.IsTrue(inv.RemoveEquipment("cloak"));
            Assert.AreEqual(0, inv.GetEquipmentCount("cloak"));
            Assert.IsFalse(inv.EquippedEquipmentIds.Contains("cloak"));
            Assert.IsFalse(inv.RemoveEquipment("cloak"));
        }

        [Test]
        public void GameClock_DayAndWeekIndices()
        {
            var monday = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(0, GameClock.DayIndex(monday));
            Assert.AreEqual(0, GameClock.WeekIndex(monday.AddDays(6).AddHours(23)));
            Assert.AreEqual(1, GameClock.WeekIndex(monday.AddDays(7)));
            Assert.AreEqual(-1, GameClock.DayIndex(monday.AddSeconds(-1)));
        }
    }
}
