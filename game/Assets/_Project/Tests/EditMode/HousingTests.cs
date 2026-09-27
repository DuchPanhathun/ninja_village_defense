using System;
using System.Collections.Generic;
using System.IO;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using NUnit.Framework;
using UnityEngine;

namespace NinjaVillage.Tests
{
    public class HousingTests
    {
        private static readonly DateTime T0 = new(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);

        // ------------------------------------------------------------------ treasury rules

        [Test]
        public void Treasury_FillsFasterAndHoldsMore_WithTheCastleAndFamilies()
        {
            Assert.AreEqual(30f, HousingRules.CoinsPerHour(1, 0));
            Assert.AreEqual(90f, HousingRules.CoinsPerHour(3, 2), "castle +15/level, families +15 each");
            Assert.AreEqual(6f, HousingRules.CapHours(1));
            Assert.AreEqual(14f, HousingRules.CapHours(5));
            Assert.AreEqual(180, HousingRules.Capacity(1, 0));
            Assert.Greater(HousingRules.Capacity(5, 6), HousingRules.Capacity(1, 0));
        }

        [Test]
        public void Treasury_AccruesOverTime_UpToItsCapacity()
        {
            long start = T0.Ticks;
            Assert.AreEqual(60.0, HousingRules.Accrued(0, start, T0.AddHours(2), 30f, 180), 0.001);
            Assert.AreEqual(180.0, HousingRules.Accrued(0, start, T0.AddHours(20), 30f, 180), 0.001, "stops when full");
            Assert.AreEqual(25.0, HousingRules.Accrued(25, start, T0.AddHours(-3), 30f, 180), 0.001, "a clock turned back adds nothing");
            Assert.AreEqual(0.0, HousingRules.Accrued(0, 0, T0, 30f, 180), 0.001, "never started: empty");
            Assert.AreEqual(TimeSpan.FromHours(4), HousingRules.UntilFull(60, 30f, 180));
            Assert.AreEqual(TimeSpan.Zero, HousingRules.UntilFull(180, 30f, 180));
        }

        // ------------------------------------------------------------------ house rules

        [Test]
        public void HousePlots_OpenOnePerCastleLevel_AndMatchTheirLabels()
        {
            Assert.AreEqual(1, HousingRules.UnlockedPlots(1));
            Assert.AreEqual(HousingRules.MaxPlots, HousingRules.UnlockedPlots(99));
            for (int plot = 0; plot < HousingRules.MaxPlots; plot++)
            {
                int castle = HousingRules.CastleLevelForPlot(plot);
                Assert.Less(plot, HousingRules.UnlockedPlots(castle));
                if (castle > 1) Assert.GreaterOrEqual(plot, HousingRules.UnlockedPlots(castle - 1));
            }
        }

        [Test]
        public void Houses_GrowWithTheCastle_AndCostMoreAsTheVillageFills()
        {
            Assert.AreEqual(1, HousingRules.LevelCap(1));
            Assert.AreEqual(2, HousingRules.LevelCap(3));
            Assert.AreEqual(HousingRules.MaxLevel, HousingRules.LevelCap(20));
            for (int level = 2; level <= HousingRules.MaxLevel; level++)
            {
                int castle = HousingRules.CastleLevelForHouseLevel(level);
                Assert.GreaterOrEqual(HousingRules.LevelCap(castle), level, $"Lv {level} is allowed at castle {castle}");
                Assert.Less(HousingRules.LevelCap(castle - 1), level, $"...and not before");
            }
            for (int built = 1; built < HousingRules.MaxPlots; built++)
                Assert.Greater(HousingRules.BuildCost(built), HousingRules.BuildCost(built - 1));
        }

        [Test]
        public void HouseStyles_AreUnique_AndSomeAreOpenFromTheStart()
        {
            var ids = new HashSet<string>();
            foreach (var style in HousingRules.Styles) Assert.IsTrue(ids.Add(style.Id), style.Id);
            Assert.GreaterOrEqual(HousingRules.Styles.Length, 6);
            Assert.AreEqual(1, HousingRules.Styles[0].RequiredCastleLevel);
        }

        [Test]
        public void HouseLots_FitInTheVillage_WithoutCoveringEachOtherOrBuildings()
        {
            var bounds = VillageLayout.Bounds;
            for (int a = 0; a < HousingRules.MaxPlots; a++)
            {
                var lot = VillageLayout.HouseLot(a);
                Assert.IsTrue(bounds.Contains(lot.min) && bounds.Contains(lot.max), $"lot {a} inside the village");
                for (int b = a + 1; b < HousingRules.MaxPlots; b++)
                    Assert.IsFalse(lot.Overlaps(VillageLayout.HouseLot(b)), $"lots {a} and {b}");
                Assert.IsFalse(lot.Overlaps(VillageLayout.FarmField), $"lot {a} vs farm");
                var catalog = VillageService.Catalog;
                if (catalog == null) continue;
                foreach (var building in catalog.All)
                {
                    Vector2 p = building.PlotPosition, f = building.Footprint;
                    var plot = new Rect(p.x - f.x * 0.75f, p.y - f.y * 0.6f, f.x * 1.5f, f.y * 1.3f);
                    Assert.IsFalse(lot.Overlaps(plot), $"lot {a} vs {building.Id}");
                }
            }
        }

        // ------------------------------------------------------------------ services (temp save, fixed clock)

        private string _dir;
        private DateTime _now;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "nv_housing_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            SaveSystem.OverrideDirectory = _dir;
            SaveService.ResetAll();
            _now = T0;
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
        public void Treasury_PaysOutWhatFlowedIn_AndAHouseRaisesTheRateFromThenOn()
        {
            Assert.AreEqual(0, TreasuryService.Available, "starts empty");
            _now = T0.AddHours(2);
            Assert.AreEqual(60, TreasuryService.Available, "30 an hour at castle 1");

            int coins = SaveService.Data.Wallet.Get(CurrencyType.Coins);
            Assert.AreEqual(60, TreasuryService.Collect());
            Assert.AreEqual(coins + 60, SaveService.Data.Wallet.Get(CurrencyType.Coins));
            Assert.AreEqual(0, TreasuryService.Available);

            _now = T0.AddHours(3); // one hour at the old rate...
            SaveService.Data.Wallet.Add(CurrencyType.Coins, 10000);
            Assert.AreEqual(HouseResult.PlotLocked, HouseService.Build(1, "tan"), "castle 1 opens one plot");
            Assert.AreEqual(HouseResult.StyleLocked, HouseService.Build(0, "igloo"));
            Assert.AreEqual(HouseResult.Success, HouseService.Build(0, "tan"));
            Assert.AreEqual(HouseResult.PlotTaken, HouseService.Build(0, "clay"));
            _now = T0.AddHours(4); // ...then one at the new one
            Assert.AreEqual(30 + 45, TreasuryService.Available);

            Assert.AreEqual(HouseResult.CastleLevel, HouseService.Upgrade(0), "Lv 2 needs castle 3");
            Assert.AreEqual(HouseResult.Success, HouseService.Restyle(0, "straw"));
            Assert.AreEqual("straw", HouseService.Get(0).Style);
        }
    }
}
