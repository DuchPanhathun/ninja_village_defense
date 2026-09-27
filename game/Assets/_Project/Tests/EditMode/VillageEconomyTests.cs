using System.Collections.Generic;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Inventory;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    public class VillageEconomyTests
    {
        // ---------------------------------------------------------------- cost curves

        [Test]
        public void CostCurve_ExponentialLinearTable()
        {
            Assert.AreEqual(100, CostCurve.Exponential(100, 1.5f, 0, 5));
            Assert.AreEqual(150, CostCurve.Exponential(100, 1.5f, 1, 5));
            Assert.AreEqual(225, CostCurve.Exponential(100, 1.5f, 2, 5));
            Assert.AreEqual(200, CostCurve.Linear(100, 50, 2, 5));
            Assert.AreEqual(30, CostCurve.FromTable(new[] { 10, 20, 30 }, 99));
            Assert.AreEqual(0, CostCurve.Exponential(0, 2f, 3));
        }

        [Test]
        public void CostCurve_RoundingNeverReturnsZeroForPositiveCosts()
        {
            Assert.AreEqual(5, CostCurve.Round(1.2, 5));
            Assert.AreEqual(125, CostCurve.Round(127, 5));
            Assert.AreEqual(0, CostCurve.Round(-3, 5));
        }

        [Test]
        public void WaveClearCoins_ScaleWithWave()
        {
            Assert.AreEqual(0, EconomyConfig.WaveClearCoins(0, 5, 3));
            Assert.AreEqual(8, EconomyConfig.WaveClearCoins(1, 5, 3));
            Assert.AreEqual(35, EconomyConfig.WaveClearCoins(10, 5, 3));
        }

        // ---------------------------------------------------------------- village rules

        [Test]
        public void CastleLevel_CapsOtherBuildings()
        {
            Assert.AreEqual(4, VillageRules.LevelCap(20, 2, 2, false));
            Assert.AreEqual(20, VillageRules.LevelCap(20, 2, 50, false));
            Assert.AreEqual(10, VillageRules.LevelCap(10, 1, 1, true), "the Castle itself is never capped by the Castle");
            Assert.AreEqual(10, VillageRules.LevelCap(10, 0, 1, false), "0 levels-per-castle = not gated");
        }

        [Test]
        public void BuildingEffect_LinearFromFirstToMaxLevel()
        {
            Assert.AreEqual(0f, VillageRules.EffectAt(0, 20, 0.02f, 0.8f), 1e-5f, "not built = no effect");
            Assert.AreEqual(0.02f, VillageRules.EffectAt(1, 20, 0.02f, 0.8f), 1e-5f);
            Assert.AreEqual(0.8f, VillageRules.EffectAt(20, 20, 0.02f, 0.8f), 1e-5f);
            Assert.AreEqual(3, VillageRules.FloorEffect(2.99999f));
        }

        [Test]
        public void UpgradeCheck_ReportsFirstBlocker()
        {
            var q = new UpgradeQuery
            {
                CurrentLevel = 2, MaxLevel = 10, IsCastle = false, CastleLevel = 1,
                RequiredCastleLevel = 1, LevelsPerCastleLevel = 2, HighestWave = 0, RequiredWave = 0,
                Cost = Price.Coins(100), Balance = 1000,
            };
            Assert.AreEqual(UpgradeBlocker.CastleLevel, VillageRules.Check(q));

            q.CastleLevel = 2;
            Assert.AreEqual(UpgradeBlocker.None, VillageRules.Check(q));

            q.Balance = 10;
            Assert.AreEqual(UpgradeBlocker.NotEnoughCurrency, VillageRules.Check(q));
        }

        // ---------------------------------------------------------------- market

        [Test]
        public void MarketRoll_IsDeterministicPerSeed_AndRespectsLevel()
        {
            var pool = new List<MarketCandidate>
            {
                new("a", 1f, 1), new("b", 1f, 1), new("c", 1f, 1), new("d", 1f, 1), new("vip", 100f, 5),
            };
            var first = new List<string>();
            var second = new List<string>();
            int seed = MarketRules.SeedFor(1000, "player");
            MarketRules.Roll(pool, 1, 3, seed, first);
            MarketRules.Roll(pool, 1, 3, seed, second);

            CollectionAssert.AreEqual(first, second, "same day + player = same stock");
            Assert.AreEqual(3, first.Count);
            CollectionAssert.AllItemsAreUnique(first);
            CollectionAssert.DoesNotContain(first, "vip", "offers above the Market level never appear");
        }

        // ---------------------------------------------------------------- inventory

        [Test]
        public void Inventory_FreshSaveGetsStarterWeaponEquipped()
        {
            var inv = new InventorySaveData();
            Assert.IsTrue(InventoryRules.EnsureDefaults(inv, "Kunai"));
            Assert.IsTrue(inv.OwnsWeapon("Kunai"));
            Assert.AreEqual("Kunai", inv.EquippedWeaponId);
            Assert.IsFalse(InventoryRules.EnsureDefaults(inv, "Kunai"));
        }

        [Test]
        public void Inventory_EquipRespectsSlots()
        {
            var inv = new InventorySaveData();
            inv.AddEquipment("a");
            inv.AddEquipment("b");
            Assert.AreEqual(EquipResult.Ok, InventoryRules.Equip(inv, "a", 1));
            Assert.AreEqual(EquipResult.AlreadyEquipped, InventoryRules.Equip(inv, "a", 1));
            Assert.AreEqual(EquipResult.NoFreeSlot, InventoryRules.Equip(inv, "b", 1));
            Assert.AreEqual(EquipResult.NotOwned, InventoryRules.Equip(inv, "zzz", 5));
            Assert.AreEqual(EquipResult.Ok, InventoryRules.Unequip(inv, "a"));
        }

        [Test]
        public void GearStacks_AreKeptPerGrade_AndTheLastCopyUnequips()
        {
            var inv = new InventorySaveData();
            inv.AddEquipment("ring", 2, 0);
            inv.AddEquipment("ring", 1, 2);
            inv.EquippedEquipmentIds.Add("ring");
            Assert.AreEqual(3, inv.GetEquipmentCount("ring"));
            Assert.AreEqual(2, inv.GetEquipmentCount("ring", 0));
            Assert.AreEqual(2, inv.BestEquipmentGrade("ring"));

            Assert.IsTrue(inv.RemoveEquipment("ring", 2, 0));
            Assert.IsTrue(inv.IsEquipmentEquipped("ring"), "an Elite copy is still owned");
            Assert.IsFalse(inv.RemoveEquipment("ring", 1, 0), "no Common copies left");
            Assert.IsTrue(inv.RemoveEquipment("ring", 1, 2));
            Assert.IsFalse(inv.IsEquipmentEquipped("ring"));
            Assert.AreEqual(-1, inv.BestEquipmentGrade("ring"));
        }
    }
}
