using System;
using System.IO;
using NinjaVillage.Core;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Crates;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Inventory;
using NinjaVillage.Systems.Save;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    /// <summary>Supply crates, Surprise Boxes and S-class equipment.</summary>
    public class CrateTests
    {
        // ------------------------------------------------------------------ rules

        [Test]
        public void OnlyTheSurpriseBox_HasAGoodChanceOfSClass_WithAPity()
        {
            var wood = CrateRules.Get("wood");
            var silver = CrateRules.Get("silver");
            var box = CrateRules.Get("surprise");
            Assert.AreEqual(0f, wood.SChance, "no S in the free crate");
            Assert.IsTrue(wood.DailyFree);
            Assert.Less(silver.SChance, box.SChance);
            Assert.Greater(box.Pity, 0);
            foreach (var crate in CrateRules.Crates)
            {
                Assert.AreEqual(5, crate.GradeWeights.Length, crate.Id);
                float total = 0f;
                foreach (var w in crate.GradeWeights) total += w;
                Assert.Greater(total, 0f, crate.Id);
            }
        }

        [Test]
        public void TheSurpriseBox_GivesSClassAboutOneInTwenty_AndSurelyAtThePity()
        {
            var box = CrateRules.Get("surprise");
            var rng = new Random(42);
            int specials = 0;
            const int rolls = 20000;
            for (int i = 0; i < rolls; i++)
                if (CrateRules.Roll(box, rng, 0).Special) specials++;
            Assert.That(specials / (float)rolls, Is.InRange(0.04f, 0.06f));

            var roll = CrateRules.Roll(box, new Random(1), box.Pity - 1);
            Assert.IsTrue(roll.Special, "the pity box is always S");
            Assert.AreEqual(ItemGrade.Elite, roll.Grade, "S items come at Elite");
            Assert.AreEqual(ItemGrade.Elite, GradeRules.Native(Rarity.Epic, special: true));
        }

        [Test]
        public void Grades_FollowTheWeights()
        {
            var weights = new[] { 70f, 25f, 5f, 0f, 0f };
            Assert.AreEqual(ItemGrade.Common, CrateRules.PickGrade(weights, 0.0));
            Assert.AreEqual(ItemGrade.Rare, CrateRules.PickGrade(weights, 0.8));
            Assert.AreEqual(ItemGrade.Elite, CrateRules.PickGrade(weights, 0.99));
        }

        // ------------------------------------------------------------------ service (temp save)

        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "nv_crates_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            SaveSystem.OverrideDirectory = _dir;
            SaveService.ResetAll();
            GameClock.OverrideUtcNow = () => new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
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
        public void SClassItems_OnlyComeFromCrates_NeverFromNormalDrops()
        {
            Assume.That(CrateService.SpecialGear().Count >= 4 && CrateService.SpecialWeapons().Count >= 2, "S content missing — run the content generator");
            foreach (var item in CrateService.SpecialGear()) Assert.AreEqual(ItemGrade.Elite, InventoryService.NativeGrade(item), item.Id);
            foreach (var item in CrateService.RegularGear()) Assert.IsFalse(item.IsSpecial);
            for (int i = 0; i < 200; i++)
            {
                var piece = InventoryService.RandomEquipmentOfRarity(Rarity.Epic);
                Assert.IsTrue(piece == null || !piece.IsSpecial, "market crates never hand out S items");
            }
            var storm = InventoryService.GetWeapon("StormNinjaku");
            Assert.IsNotNull(storm);
            Assert.AreEqual(ForgeBlocker.NotCraftable, ForgeService.CheckCraft(storm), "S weapons can't be crafted");
        }

        [Test]
        public void TheFreeCrate_OpensOnceADay_PaidOnesCost_AndThePityHandsOutAnSItem()
        {
            Assume.That(CrateService.SpecialGear().Count > 0, "S content missing — run the content generator");
            Assert.IsTrue(CrateService.FreeAvailable);
            Assert.AreEqual(CrateResult.Opened, CrateService.Open("wood", 1, true, out var free, new Random(3)));
            Assert.AreEqual(1, free.Count);
            Assert.IsFalse(free[0].Special);
            Assert.AreEqual(CrateResult.FreeUsed, CrateService.Open("wood", 1, true, out _));
            Assert.IsFalse(CrateService.FreeAvailable);

            var wallet = SaveService.Data.Wallet;
            wallet.Add(CurrencyType.Gems, 2000);
            int gems = wallet.Get(CurrencyType.Gems);
            Assert.AreEqual(CrateResult.Opened, CrateService.Open("silver", CrateRules.MultiOpen, false, out var ten, new Random(5)));
            Assert.AreEqual(CrateRules.MultiOpen, ten.Count);
            Assert.AreEqual(gems - CrateRules.Get("silver").Price.Amount * CrateRules.MultiOpen, wallet.Get(CurrencyType.Gems));

            var box = CrateRules.Get("surprise");
            SaveService.Data.Crates.SurprisePity = box.Pity - 1;
            Assert.AreEqual(1, CrateService.PityLeft(box));
            Assert.AreEqual(CrateResult.Opened, CrateService.Open("surprise", 1, false, out var lucky, new Random(9)));
            Assert.IsTrue(lucky[0].Special, "certain at the pity");
            Assert.AreEqual(ItemGrade.Elite, lucky[0].Grade);
            Assert.AreEqual(0, SaveService.Data.Crates.SurprisePity, "the pity resets");
            bool owned = lucky[0].Weapon ? InventoryService.Data.OwnsWeapon(lucky[0].Id) : InventoryService.Data.GetEquipmentCount(lucky[0].Id) > 0;
            Assert.IsTrue(owned, "the S item is yours");

            wallet.TrySpend(CurrencyType.Gems, wallet.Get(CurrencyType.Gems));
            Assert.AreEqual(CrateResult.NotEnoughCurrency, CrateService.Open("surprise", 1, false, out _));
        }
    }
}
