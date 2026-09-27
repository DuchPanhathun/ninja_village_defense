using System;
using System.IO;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Pets;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    /// <summary>EPIC 24 Phase 7: day / night, weather and pet care.</summary>
    public class VillageLifeTests
    {
        private static DateTime At(int hour, int minute = 0) => new(2026, 6, 10, hour, minute, 0, DateTimeKind.Local);

        [Test]
        public void TheSky_FollowsThePhonesClock()
        {
            Assert.AreEqual(0f, AtmosphereRules.SkyTint(At(12)).a, 0.001f, "clear at noon");
            Assert.AreEqual(0f, AtmosphereRules.Darkness(At(12)), 0.001f);
            var night = AtmosphereRules.SkyTint(At(23));
            Assert.Greater(night.a, 0.4f);
            Assert.Greater(night.b, night.r, "night is blue");
            var dusk = AtmosphereRules.SkyTint(At(18, 30));
            Assert.Greater(dusk.r, dusk.b, "dusk is orange");
            Assert.AreEqual(1f, AtmosphereRules.Darkness(At(2)), 0.001f);
            Assert.Less(AtmosphereRules.Darkness(At(6)), 0.4f, "dawn is only half-light");
            Assert.IsTrue(AtmosphereRules.IsNight(At(22)));
            Assert.IsFalse(AtmosphereRules.IsNight(At(9)));
        }

        [Test]
        public void Weather_IsSteadyForThreeHours_WetNowAndThen_AndSnowsOnlyInWinter()
        {
            Assert.AreEqual(AtmosphereRules.WeatherAt(At(13)), AtmosphereRules.WeatherAt(At(14, 59)), "one 3-hour block");
            int blocks = 0, wet = 0;
            for (var day = new DateTime(2026, 1, 1); day.Year == 2026; day = day.AddDays(1))
                for (int hour = 0; hour < 24; hour += 3)
                {
                    var weather = AtmosphereRules.WeatherAt(day.AddHours(hour));
                    blocks++;
                    if (weather == Weather.Clear) continue;
                    wet++;
                    bool winter = day.Month == 12 || day.Month <= 2;
                    Assert.AreEqual(winter ? Weather.Snow : Weather.Rain, weather, day.ToString("MMM"));
                }
            Assert.That(wet / (float)blocks, Is.InRange(0.12f, 0.24f), "about one block in six");
        }

        [Test]
        public void HappyPets_AreStrongerToday()
        {
            Assert.AreEqual(1.15f, PetCareRules.PowerScale(100, 100, 100), 0.0001f);
            Assert.AreEqual(1.05f, PetCareRules.PowerScale(100, 99, 100), 0.0001f, "fed yesterday doesn't count");
            Assert.AreEqual(1f, PetCareRules.PowerScale(0, 0, 100));
            Assert.IsFalse(PetCareRules.CanPet(100, 100));
            Assert.IsTrue(PetCareRules.CanFeed(99, 100));
            Assert.AreEqual("carrot", PetCareRules.PickTreat(id => id == "carrot" || id == "rice" ? 1 : 0), "fish first, then crops");
            Assert.IsNull(PetCareRules.PickTreat(_ => 0));
        }

        // ------------------------------------------------------------------ services (temp save, fixed clock)

        private string _dir;
        private DateTime _now;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "nv_life_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            SaveSystem.OverrideDirectory = _dir;
            SaveService.ResetAll();
            _now = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);
            GameClock.OverrideUtcNow = () => _now;
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
        public void PettingAndFeeding_WorkOnceADay_AndFeedingUsesATreat()
        {
            const string pet = "test_pet";
            Assert.AreEqual(PetCareResult.NotOwned, PetCareService.Pet(pet));
            SaveService.Data.Pets.Owned.SetLevel(pet, 1);
            Assert.AreEqual(PetCareResult.Success, PetCareService.Pet(pet));
            Assert.AreEqual(PetCareResult.AlreadyToday, PetCareService.Pet(pet));
            Assert.AreEqual(PetCareResult.NoTreat, PetCareService.Feed(pet, out _));
            GoodsService.Add("fish", 1);
            Assert.AreEqual(PetCareResult.Success, PetCareService.Feed(pet, out var treat));
            Assert.AreEqual("fish", treat);
            Assert.AreEqual(0, GoodsService.Count("fish"));
            Assert.AreEqual(1.15f, PetCareService.PowerScale(SaveService.Data, pet), 0.0001f);

            _now = _now.AddDays(1);
            Assert.IsTrue(PetCareService.CanPet(pet), "a new day");
            Assert.AreEqual(1f, PetCareService.PowerScale(SaveService.Data, pet), 0.0001f);
        }

        [Test]
        public void Rain_WatersEveryGrowingCrop_Once()
        {
            SaveService.Data.Wallet.Add(NinjaVillage.Systems.Economy.CurrencyType.Coins, 1000);
            var rice = FarmService.GetCrop("rice");
            Assume.That(rice != null, "crop catalog missing — run the content generator");
            FarmService.Plant(0, rice);
            FarmService.Plant(1, rice);
            FarmService.Water(1);
            Assert.AreEqual(1, FarmService.WaterAllByRain(), "the dry one");
            Assert.IsTrue(FarmService.GetPlot(0).Watered);
            Assert.AreEqual(0, FarmService.WaterAllByRain(), "nothing left to water");
        }
    }
}
