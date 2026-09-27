using NinjaVillage.Systems.Backend;
using NinjaVillage.Systems.Save;
using NinjaVillage.UI.Common;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    public class PublicVillageTests
    {
        private static SaveData SampleSave()
        {
            var save = new SaveData();
            save.Migrate();
            save.Profile.DisplayName = "Kage";
            save.Profile.HighestWaveReached = 18;
            save.Profile.TotalKills = 655;
            save.Chapters.HighestCleared = 1;
            save.Village.Buildings.SetLevel("castle", 4);
            save.Village.Decorations.Add(new PlacedDecoration { Uid = 1, Id = "well", X = 3f, Y = -5f });
            save.Heroes.Owned.SetLevel("assassin", 5);
            save.Heroes.SelectedHeroId = "assassin";
            return save;
        }

        [Test]
        public void PublicVillage_CarriesTheHeadlinesAndTheWholeVillage()
        {
            var village = BackendRules.BuildPublicVillage(SampleSave(), "uid-1");
            Assert.AreEqual("uid-1", village.UserId);
            Assert.AreEqual("Kage", village.DisplayName);
            Assert.AreEqual(4, village.CastleLevel);
            Assert.AreEqual(18, village.HighestWave);
            Assert.AreEqual(1, village.ChaptersCleared);

            var snapshot = VillageVisitFlow.Parse(village);
            Assert.IsNotNull(snapshot);
            Assert.AreEqual("uid-1", snapshot.PlayerId);
            Assert.AreEqual("well", snapshot.Decorations[0].Id);
            Assert.AreEqual("assassin", snapshot.SelectedHeroId);
            Assert.AreEqual(655, snapshot.TotalKills);
        }

        [Test]
        public void PublicVillage_IsIdenticalWhenNothingChanged_SoItIsNotReuploaded()
        {
            var save = SampleSave();
            Assert.AreEqual(BackendRules.BuildPublicVillage(save, "u").SnapshotJson, BackendRules.BuildPublicVillage(save, "u").SnapshotJson);
            save.Village.Decorations[0].X = 4f;
            Assert.AreNotEqual(BackendRules.BuildPublicVillage(SampleSave(), "u").SnapshotJson, BackendRules.BuildPublicVillage(save, "u").SnapshotJson);
        }

        [Test]
        public void Parse_RejectsMissingOrBrokenVillages()
        {
            Assert.IsNull(VillageVisitFlow.Parse(null));
            Assert.IsNull(VillageVisitFlow.Parse(new PublicVillage { UserId = "x", SnapshotJson = "" }));
            Assert.IsNull(VillageVisitFlow.Parse(new PublicVillage { UserId = "x", SnapshotJson = "not json" }));
        }

        [Test]
        public void OfflineProvider_KeepsPublishedVillagesNewestFirst()
        {
            var offline = new OfflineBackendProvider();
            Assert.IsTrue(offline.WriteVillageAsync(new PublicVillage { UserId = "a", DisplayName = "A" }).Result);
            System.Threading.Thread.Sleep(5);
            Assert.IsTrue(offline.WriteVillageAsync(new PublicVillage { UserId = "b", DisplayName = "B" }).Result);

            Assert.AreEqual("A", offline.LoadVillageAsync("a").Result.DisplayName);
            Assert.IsNull(offline.LoadVillageAsync("nobody").Result);
            var list = offline.ListVillagesAsync(10).Result;
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("b", list[0].UserId);
        }
    }
}
