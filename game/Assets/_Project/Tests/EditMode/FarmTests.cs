using System;
using NinjaVillage.Systems.Farm;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    public class FarmTests
    {
        private static readonly DateTime T0 = new(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Plots_OpenWithTheCastle_AndMatchTheirLabels()
        {
            Assert.AreEqual(2, FarmRules.UnlockedPlots(1));
            Assert.AreEqual(3, FarmRules.UnlockedPlots(2));
            Assert.AreEqual(FarmRules.MaxPlots, FarmRules.UnlockedPlots(99));
            for (int plot = 0; plot < FarmRules.MaxPlots; plot++)
            {
                int castle = FarmRules.CastleLevelForPlot(plot);
                Assert.Less(plot, FarmRules.UnlockedPlots(castle), $"plot {plot} opens at castle {castle}");
                if (castle > 1) Assert.GreaterOrEqual(plot, FarmRules.UnlockedPlots(castle - 1), $"plot {plot} isn't open earlier");
            }
        }

        [Test]
        public void Crops_GoSeed_Growing_Ripe()
        {
            long planted = T0.Ticks, ready = T0.AddMinutes(10).Ticks;
            Assert.AreEqual(CropStage.Empty, FarmRules.Stage(T0, 0, 0));
            Assert.AreEqual(CropStage.Seed, FarmRules.Stage(T0.AddMinutes(1), planted, ready));
            Assert.AreEqual(CropStage.Growing, FarmRules.Stage(T0.AddMinutes(5), planted, ready));
            Assert.AreEqual(CropStage.Ripe, FarmRules.Stage(T0.AddMinutes(10), planted, ready));
            Assert.AreEqual(0.5f, FarmRules.Progress(T0.AddMinutes(5), planted, ready), 0.001f);
        }

        [Test]
        public void Watering_CutsTheTimeLeftBy30Percent()
        {
            long ready = T0.AddMinutes(100).Ticks;
            long watered = FarmRules.WaterReadyTicks(T0, ready);
            Assert.AreEqual(70.0, TimeSpan.FromTicks(watered - T0.Ticks).TotalMinutes, 0.01);
            Assert.AreEqual(ready, FarmRules.WaterReadyTicks(T0.AddMinutes(200), ready), "a ripe crop isn't changed");
        }

        [Test]
        public void TimeLabels_AreShort()
        {
            Assert.AreEqual("4h 05m", FarmRules.Format(new TimeSpan(4, 5, 30)));
            Assert.AreEqual("12m 03s", FarmRules.Format(new TimeSpan(0, 12, 3)));
            Assert.AreEqual("45s", FarmRules.Format(TimeSpan.FromSeconds(44.2)));
        }
    }
}
