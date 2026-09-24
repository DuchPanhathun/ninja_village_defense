using System.Collections.Generic;
using NinjaVillage.Systems.Heroes;
using NinjaVillage.Systems.Pets;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Talents;
using NUnit.Framework;

namespace NinjaVillage.Tests
{
    public class MetaProgressionTests
    {
        // ---------------------------------------------------------------- heroes

        [Test]
        public void HeroLevelCap_GrowsWithDojo_AndRespectsHeroMax()
        {
            Assert.AreEqual(HeroRules.BaseLevelCap, HeroRules.GetLevelCap(0, 30));
            Assert.AreEqual(HeroRules.BaseLevelCap + 2 * HeroRules.LevelsPerDojoLevel, HeroRules.GetLevelCap(2, 30));
            Assert.AreEqual(30, HeroRules.GetLevelCap(100, 30));
            Assert.AreEqual(0, HeroRules.GetRequiredDojoLevel(HeroRules.BaseLevelCap));
            Assert.AreEqual(1, HeroRules.GetRequiredDojoLevel(HeroRules.BaseLevelCap + 1));
        }

        [Test]
        public void HeroUpgradeCost_IsMonotonicAndRounded()
        {
            int previous = 0;
            for (int level = 1; level < 20; level++)
            {
                int cost = HeroRules.GetUpgradeCost(level, 120, 1.6f);
                Assert.Greater(cost, previous);
                Assert.AreEqual(0, cost % HeroRules.CostRounding);
                previous = cost;
            }
            Assert.AreEqual(0, HeroRules.GetUpgradeCost(3, 0, 1.6f));
        }

        [Test]
        public void HeroUnlockChecks_OrderOfBlockers()
        {
            Assert.AreEqual(HeroActionResult.AlreadyUnlocked, HeroRules.CheckUnlock(true, 5, 0, 9999, 10));
            Assert.AreEqual(HeroActionResult.DojoLevelTooLow, HeroRules.CheckUnlock(false, 1, 2, 9999, 10));
            Assert.AreEqual(HeroActionResult.NotEnoughCurrency, HeroRules.CheckUnlock(false, 5, 2, 5, 10));
            Assert.AreEqual(HeroActionResult.Success, HeroRules.CheckUnlock(false, 5, 2, 10, 10));
        }

        [Test]
        public void StarterHeroes_GrantedOnceAndSelected()
        {
            var save = new HeroSaveData();
            Assert.IsTrue(HeroRules.EnsureStarterHeroes(save, new List<string> { "assassin" }));
            Assert.IsTrue(save.IsUnlocked("assassin"));
            Assert.AreEqual("assassin", save.SelectedHeroId);
            Assert.IsFalse(HeroRules.EnsureStarterHeroes(save, new List<string> { "assassin" }), "second call changes nothing");
        }

        [Test]
        public void BeastNinjaPack_GrowsWithLevel_Capped()
        {
            Assert.AreEqual(1, HeroRules.GetCompanionCount(1, 10, 1, 3));
            Assert.AreEqual(2, HeroRules.GetCompanionCount(1, 10, 10, 3));
            Assert.AreEqual(3, HeroRules.GetCompanionCount(1, 10, 30, 3));
            Assert.AreEqual(3, HeroRules.GetCompanionCount(1, 10, 99, 3));
            Assert.AreEqual(1, HeroRules.GetCompanionLevel(1, 20));
            Assert.AreEqual(6, HeroRules.GetCompanionLevel(11, 20));
        }

        // ---------------------------------------------------------------- pets

        [Test]
        public void PetHouse_GatesSlotsAndLevels()
        {
            Assert.AreEqual(1, PetRules.GetMaxOwnedPets(0));
            Assert.AreEqual(4, PetRules.GetMaxOwnedPets(3));
            Assert.AreEqual(PetRules.BaseLevelCap, PetRules.GetLevelCap(0, 20));
            Assert.AreEqual(20, PetRules.GetLevelCap(50, 20));
            Assert.AreEqual(PetActionResult.PetSlotsFull, PetRules.CheckUnlock(false, 1, 0, 0, 9999, 10));
            Assert.AreEqual(PetActionResult.Success, PetRules.CheckUnlock(false, 1, 0, 0, 9999, 10, bypassSlotLimit: true));
            Assert.AreEqual(PetActionResult.PetHouseLevelTooLow, PetRules.CheckUnlock(false, 0, 1, 3, 9999, 10));
        }

        [Test]
        public void PetSave_EquipmentMovesBetweenPets()
        {
            var pets = new PetSaveData();
            pets.AddEquipment("collar");
            pets.SetEquippedItem("fox", "collar");
            Assert.AreEqual("collar", pets.GetEquippedItem("fox"));

            pets.SetEquippedItem("wolf", "collar");
            Assert.AreEqual("collar", pets.GetEquippedItem("wolf"));
            Assert.IsNull(pets.GetEquippedItem("fox"), "an item can only be worn by one pet");

            pets.SetEquippedItem("wolf", null);
            Assert.IsNull(pets.GetEquippedItem("wolf"));
        }

        // ---------------------------------------------------------------- talents

        [Test]
        public void TalentRankCost_GrowsAndRoundsToFive()
        {
            Assert.AreEqual(150, TalentRules.RankCost(150, 1.5f, 0));
            Assert.AreEqual(225, TalentRules.RankCost(150, 1.5f, 1));
            Assert.AreEqual(0, TalentRules.RankCost(150, 1.5f, 3) % 5);
            Assert.AreEqual(150 + 225, TalentRules.TotalCostUpTo(150, 1.5f, 2));
        }

        [Test]
        public void TalentRankUp_BlockerOrder()
        {
            Assert.AreEqual(TalentResult.MaxRank, TalentRules.CheckRankUp(5, 5, true, 9999, 10));
            Assert.AreEqual(TalentResult.PrerequisitesMissing, TalentRules.CheckRankUp(0, 5, false, 9999, 10));
            Assert.AreEqual(TalentResult.NotEnoughCoins, TalentRules.CheckRankUp(0, 5, true, 5, 10));
            Assert.AreEqual(TalentResult.Success, TalentRules.CheckRankUp(0, 5, true, 10, 10));
        }

        [Test]
        public void TalentReset_FirstFreeThenCostsGems()
        {
            Assert.AreEqual(0, TalentRules.ResetGemCost(0));
            Assert.AreEqual(TalentRules.ResetGemBase, TalentRules.ResetGemCost(1));
            Assert.AreEqual(TalentRules.ResetGemCap, TalentRules.ResetGemCost(1000));
            Assert.AreEqual(TalentResult.NothingToReset, TalentRules.CheckReset(0, 0, 0));
            Assert.AreEqual(TalentResult.Success, TalentRules.CheckReset(3, 0, 0));
            Assert.AreEqual(TalentResult.NotEnoughGems, TalentRules.CheckReset(3, 1, TalentRules.ResetGemBase - 1));
        }
    }
}
