using System;
using System.IO;
using NinjaVillage.Core;
using NinjaVillage.Systems.Inventory;
using NinjaVillage.Systems.Save;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    /// <summary>Weapon and gear grades (Common → Rare → Elite → Epic → Legendary) and merging 3 → 1.</summary>
    public class GradeTests
    {
        // ------------------------------------------------------------------ rules

        [Test]
        public void FiveGrades_InOrder_WithEliteBetweenRareAndEpic()
        {
            CollectionAssert.AreEqual(new[] { "Common", "Rare", "Elite", "Epic", "Legendary" }, Enum.GetNames(typeof(ItemGrade)));
            Assert.AreEqual(ItemGrade.Common, GradeRules.Native(Rarity.Common));
            Assert.AreEqual(ItemGrade.Rare, GradeRules.Native(Rarity.Rare));
            Assert.AreEqual(ItemGrade.Epic, GradeRules.Native(Rarity.Epic));
            Assert.AreEqual(ItemGrade.Legendary, GradeRules.Native(Rarity.Legendary));
            Assert.AreEqual(ItemGrade.Elite, GradeRules.Next(ItemGrade.Rare));
            Assert.AreEqual(ItemGrade.Legendary, GradeRules.Next(ItemGrade.Legendary), "nothing above Legendary");
        }

        [Test]
        public void HigherGrades_AreStronger_AndAnItemsNativeGradeKeepsItsListedStats()
        {
            Assert.AreEqual(1f, GradeRules.StatScale(ItemGrade.Legendary, ItemGrade.Legendary), 0.0001f, "listed stats at the native grade");
            Assert.AreEqual(1f, GradeRules.StatScale(ItemGrade.Common, ItemGrade.Common), 0.0001f);
            for (int g = 1; g <= (int)GradeRules.Max; g++)
            {
                Assert.Greater(GradeRules.StatScale(ItemGrade.Common, (ItemGrade)g), GradeRules.StatScale(ItemGrade.Common, (ItemGrade)(g - 1)));
                Assert.Greater(GradeRules.WeaponAttackBonus((ItemGrade)g), GradeRules.WeaponAttackBonus((ItemGrade)(g - 1)));
            }
            Assert.Less(GradeRules.StatScale(ItemGrade.Legendary, ItemGrade.Common), 1f, "a Legendary item at Common is weaker");
        }

        [Test]
        public void ThreeCopiesMerge_OrTwoPlusBars()
        {
            var (_, bars) = GradeRules.BarsFor(ItemGrade.Rare);
            Assert.IsTrue(GradeRules.CanMerge(ItemGrade.Common, 3, 0));
            Assert.IsFalse(GradeRules.CanMerge(ItemGrade.Common, 2, bars - 1));
            Assert.IsTrue(GradeRules.CanMerge(ItemGrade.Common, 2, bars), "bars make up one missing copy");
            Assert.IsFalse(GradeRules.CanMerge(ItemGrade.Common, 1, 999), "...but only one");
            Assert.IsFalse(GradeRules.CanMerge(ItemGrade.Legendary, 9, 999), "Legendary is the top");
        }

        // ------------------------------------------------------------------ services (temp save)

        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "nv_grades_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            SaveSystem.OverrideDirectory = _dir;
            SaveService.ResetAll();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.OverrideDirectory = null;
            SaveService.Load();
            try { Directory.Delete(_dir, true); } catch { /* best effort */ }
        }

        [Test]
        public void OldSaves_KeepTheirStrength()
        {
            var ring = InventoryService.GetEquipment("iron_ring");
            var scale = InventoryService.GetEquipment("dragon_scale");
            Assume.That(ring != null && scale != null, "equipment catalog missing — run the content generator");

            // A save from before grades: stacks without a grade, a weapon reforged to Golden.
            var inv = SaveService.Data.Inventory;
            inv.GradeVersion = 0;
            inv.Equipment.Add(new OwnedEquipment { Id = "iron_ring", Count = 4 });
            inv.Equipment.Add(new OwnedEquipment { Id = "dragon_scale", Count = 2 });
            inv.Weapons.SetLevel("Kunai", 5);
            inv.WeaponTiers.SetLevel("Kunai", 2);
            inv.WeaponCopies.Clear();

            inv = InventoryService.Data; // migrates
            Assert.AreEqual(1, inv.GradeVersion);
            Assert.AreEqual(4, inv.GetEquipmentCount("iron_ring", (int)ItemGrade.Common));
            Assert.AreEqual(2, inv.GetEquipmentCount("dragon_scale", (int)ItemGrade.Legendary), "a Dragon Scale stays Legendary");
            Assert.AreEqual(ItemGrade.Epic, InventoryService.WeaponGrade("Kunai"), "Golden → Epic");
            Assert.AreEqual(5, inv.GetWeaponLevel("Kunai"), "levels stay");
        }

        [Test]
        public void MergingGear_MakesTheEquippedPieceStronger_AndMergeAllCascades()
        {
            var ring = InventoryService.GetEquipment("iron_ring");
            Assume.That(ring != null, "equipment catalog missing — run the content generator");
            InventoryService.AddEquipment("iron_ring", 9); // native Common
            Assert.AreEqual(EquipResult.Ok, InventoryService.EquipEquipment("iron_ring"));
            int before = LoadoutPower.Compute(SaveService.Data).Attack;

            Assert.AreEqual(MergeResult.Merged, MergeService.MergeGear("iron_ring", ItemGrade.Common));
            var inv = InventoryService.Data;
            Assert.AreEqual(6, inv.GetEquipmentCount("iron_ring", 0));
            Assert.AreEqual(1, inv.GetEquipmentCount("iron_ring", 1));
            Assert.AreEqual(ItemGrade.Rare, InventoryService.GearGrade(ring), "equipped pieces use the best grade");
            Assert.IsTrue(inv.IsEquipmentEquipped("iron_ring"));
            Assert.Greater(LoadoutPower.Compute(SaveService.Data).Attack, before, "a Rare ring hits harder");

            // 6 Common + 1 Rare → 2 more Rare → 3 Rare → 1 Elite.
            Assert.AreEqual(3, MergeService.MergeAll());
            Assert.AreEqual(0, inv.GetEquipmentCount("iron_ring", 0));
            Assert.AreEqual(0, inv.GetEquipmentCount("iron_ring", 1));
            Assert.AreEqual(1, inv.GetEquipmentCount("iron_ring", 2));
            Assert.AreEqual(ItemGrade.Elite, InventoryService.GearGrade(ring));
            Assert.AreEqual(0, MergeService.MergeableCount());
            Assert.AreEqual(MergeResult.NotEnoughCopies, MergeService.MergeGear("iron_ring", ItemGrade.Elite));
        }

        [Test]
        public void TheForge_MakesWeaponCopies_ThatMergeIntoABetterGrade()
        {
            var kunai = InventoryService.GetWeapon(InventoryService.StarterWeaponId);
            Assume.That(kunai != null, "weapon catalog missing");
            SaveService.Data.Village.Buildings.SetLevel(BuildingIds.Forge, 1);
            SaveService.Data.Wallet.Add(NinjaVillage.Systems.Economy.CurrencyType.Coins, 10000);
            Assert.AreEqual(1, InventoryService.Data.GetWeaponCopies(kunai.Id, 0), "the starter copy");
            Assert.IsTrue(ForgeService.TryCraft(kunai, out _));
            Assert.IsTrue(ForgeService.TryCraft(kunai, out _));
            Assert.AreEqual(3, InventoryService.Data.GetWeaponCopies(kunai.Id, 0), "crafting an owned weapon forges a copy");

            int before = LoadoutPower.Compute(SaveService.Data).Attack;
            Assert.AreEqual(MergeResult.Merged, MergeService.MergeWeapon(kunai.Id, ItemGrade.Common));
            Assert.AreEqual(ItemGrade.Rare, InventoryService.WeaponGrade(kunai.Id));
            Assert.AreEqual(kunai.Id, InventoryService.Data.EquippedWeaponId, "still equipped");
            Assert.Greater(LoadoutPower.Compute(SaveService.Data).Attack, before, "+15% attack at Rare");
        }
    }
}
