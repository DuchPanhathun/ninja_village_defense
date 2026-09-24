using System;
using System.IO;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Inventory;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    public class PondMineTests
    {
        private static readonly DateTime T0 = new(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);

        // ------------------------------------------------------------------ fishing rules

        [Test]
        public void Casts_StartFull_AndRefillOneEvery20Minutes()
        {
            Assert.AreEqual(PondMineRules.MaxCasts, PondMineRules.Casts(-1, 0, T0).casts, "never fished: a full set");
            var (casts, regen) = PondMineRules.Casts(2, T0.Ticks, T0.AddMinutes(30));
            Assert.AreEqual(3, casts);
            Assert.AreEqual(T0.AddMinutes(20).Ticks, regen, "the half-refilled cast keeps its progress");
            Assert.AreEqual(PondMineRules.MaxCasts, PondMineRules.Casts(3, T0.Ticks, T0.AddHours(5)).casts, "never over the max");
            Assert.AreEqual(2, PondMineRules.Casts(2, T0.Ticks, T0.AddHours(-2)).casts, "a clock turned back earns nothing");
            Assert.AreEqual(TimeSpan.FromMinutes(15), PondMineRules.UntilNextCast(3, T0.Ticks, T0.AddMinutes(5)));
            Assert.AreEqual(TimeSpan.Zero, PondMineRules.UntilNextCast(PondMineRules.MaxCasts, T0.Ticks, T0));
        }

        [Test]
        public void RarerFish_NeedABiggerCastle_AndAreHarderToLand()
        {
            for (double roll = 0; roll < 1; roll += 0.01)
                Assert.AreEqual(FishRarity.Common, PondMineRules.PickFish(roll, 1).Rarity, "castle 1: only common fish");
            Assert.AreEqual("fish", PondMineRules.PickFish(0, 3).Id);
            Assert.AreEqual("golden_koi", PondMineRules.PickFish(0.9999, 3).Id);

            Assert.Greater(PondMineRules.ZoneWidth(0.1f), PondMineRules.ZoneWidth(0.9f));
            Assert.Less(PondMineRules.MarkerSpeed(0.1f), PondMineRules.MarkerSpeed(0.9f));
            Assert.AreEqual(CatchResult.Perfect, PondMineRules.Judge(0.5f, 0.5f, 0.3f));
            Assert.AreEqual(CatchResult.Catch, PondMineRules.Judge(0.62f, 0.5f, 0.3f));
            Assert.AreEqual(CatchResult.Miss, PondMineRules.Judge(0.7f, 0.5f, 0.3f));
            Assert.AreEqual(0.4f, PondMineRules.MarkerPosition(0.2f, 2f), 0.0001f);
            Assert.AreEqual(0.6f, PondMineRules.MarkerPosition(0.7f, 2f), 0.0001f, "bounces back");
        }

        // ------------------------------------------------------------------ mine rules

        [Test]
        public void Mine_SplitsBarsByLevel_CarryingTheFractions()
        {
            double gold = 0, mithril = 0, gems = 0;
            var haul = PondMineRules.Split(10, 1, ref gold, ref mithril, ref gems);
            Assert.AreEqual(10, haul.Iron, "Lv 1: iron only");

            gold = mithril = gems = 0;
            Assert.AreEqual(0, PondMineRules.Split(3, 3, ref gold, ref mithril, ref gems).Gold, "0.6 of a gold bar so far");
            Assert.AreEqual(1, PondMineRules.Split(3, 3, ref gold, ref mithril, ref gems).Gold, "...1.2 now");

            gold = mithril = gems = 0;
            haul = PondMineRules.Split(100, 5, ref gold, ref mithril, ref gems);
            Assert.AreEqual(20, haul.Gold);
            Assert.AreEqual(8, haul.Mithril);
            Assert.AreEqual(72, haul.Iron);
            Assert.AreEqual(4, haul.Gems, "a gem every 25 bars");
            Assert.AreEqual(100, haul.Bars);
        }

        [Test]
        public void EachTier_ReforgesWithItsOwnBar()
        {
            Assert.AreEqual("iron_bar", PondMineRules.ReforgeBar(1));
            Assert.AreEqual("gold_bar", PondMineRules.ReforgeBar(2));
            Assert.AreEqual("mithril_bar", PondMineRules.ReforgeBar(3));
            Assert.IsNull(PondMineRules.ReforgeBar(0));
        }

        // ------------------------------------------------------------------ services (temp save, fixed clock)

        private string _dir;
        private DateTime _now;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "nv_pondmine_" + Guid.NewGuid().ToString("N"));
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
        public void Fishing_UsesCasts_AndLandsFishOrAKoiPond()
        {
            Assert.AreEqual(PondMineRules.MaxCasts, FishingService.Casts);
            for (int i = 0; i < PondMineRules.MaxCasts; i++) Assert.IsTrue(FishingService.TryCast(out _, new Random(i)));
            Assert.IsFalse(FishingService.TryCast(out _), "out of casts");
            _now = T0.AddMinutes(PondMineRules.CastRegenMinutes);
            Assert.AreEqual(1, FishingService.Casts, "one back after 20 minutes");

            var shrimp = PondMineRules.GetFish("shrimp").Value;
            Assert.AreEqual(0, FishingService.Land(shrimp, CatchResult.Miss));
            Assert.AreEqual(2, FishingService.Land(shrimp, CatchResult.Perfect));
            Assert.AreEqual(2, GoodsService.Count("shrimp"));

            var koiPond = DecorationService.Get(PondMineRules.KoiDecorationId);
            Assume.That(koiPond != null, "decoration catalog missing — run the content generator");
            Assert.IsTrue(koiPond.GiftOnly, "not sold in the shop");
            Assert.IsFalse(DecorationService.GetShopItems().Contains(koiPond));
            FishingService.Land(PondMineRules.GetFish("golden_koi").Value, CatchResult.Catch);
            Assert.AreEqual(1, DecorationService.GiftCount(koiPond), "a Golden Koi becomes a pond to place");
        }

        [Test]
        public void Mine_DigsBarsOverTime_AndTheForgeTakesThemForMissingGear()
        {
            var village = SaveService.Data.Village;
            Assert.AreEqual(0, MineService.Available, "no mine, no bars");
            village.Buildings.SetLevel(BuildingIds.Castle, 3);
            village.Buildings.SetLevel(BuildingIds.Mine, 1);
            Assume.That(VillageService.Get(BuildingIds.Mine) != null, "building catalog missing — run the content generator");
            Assert.AreEqual(0, MineService.Available, "starts digging when first looked at");
            _now = T0.AddHours(2);
            Assert.AreEqual(8, MineService.Available);
            _now = T0.AddHours(30);
            Assert.AreEqual(MineService.Capacity, MineService.Available, "stops when full");
            var haul = MineService.Collect();
            Assert.AreEqual(MineService.Capacity, haul.Iron);
            Assert.AreEqual(haul.Iron, GoodsService.Count("iron_bar"));

            // Reforge Kunai to Steel with no spare gear: bars stand in for both pieces.
            var kunai = InventoryService.GetWeapon(InventoryService.StarterWeaponId);
            Assume.That(kunai != null, "weapon catalog missing");
            var inv = InventoryService.Data;
            inv.Equipment.Clear();
            inv.Weapons.SetLevel(kunai.Id, 4);
            village.Buildings.SetLevel(BuildingIds.Forge, 3);
            SaveService.Data.Wallet.Add(CurrencyType.Coins, 5000);
            GoodsService.TrySpend("iron_bar", GoodsService.Count("iron_bar"));
            GoodsService.Add("iron_bar", 2 * PondMineRules.BarsPerMaterial - 1);
            Assert.AreEqual(ForgeBlocker.NotEnoughMaterials, ForgeService.CheckReforge(kunai), "one bar short");
            GoodsService.Add("iron_bar", 1);
            Assert.AreEqual(ForgeBlocker.None, ForgeService.CheckReforge(kunai));
            Assert.IsTrue(ForgeService.TryReforge(kunai, out _));
            Assert.AreEqual(1, ForgeService.GetTier(kunai.Id), "Steel");
            Assert.AreEqual(0, GoodsService.Count("iron_bar"), "the bars were used");
        }
    }
}
