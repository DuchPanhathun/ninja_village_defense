using System;

namespace NinjaVillage.Systems.Pets
{
    /// <summary>
    /// The pet system's gates and prices as pure functions (unit-tested in PetTests). The Pet House
    /// (EPIC 11 "Pet management") drives both limits:
    /// <list type="bullet">
    /// <item>Pets you can own = <see cref="BasePetSlots"/> + Pet House level × <see cref="PetSlotsPerPetHouseLevel"/>
    /// (one pet even before the Pet House is built).</item>
    /// <item>Pet level cap = <see cref="BaseLevelCap"/> + Pet House level × <see cref="LevelsPerPetHouseLevel"/>, never above the pet's max.</item>
    /// <item>Upgrade L → L+1 = base × L^exponent coins, rounded to 10.</item>
    /// </list>
    /// </summary>
    public static class PetRules
    {
        public const int BasePetSlots = 1;
        public const int PetSlotsPerPetHouseLevel = 1;
        public const int BaseLevelCap = 5;
        public const int LevelsPerPetHouseLevel = 3;
        public const int CostRounding = 10;

        public static int GetMaxOwnedPets(int petHouseLevel) =>
            BasePetSlots + Math.Max(0, petHouseLevel) * PetSlotsPerPetHouseLevel;

        public static int GetLevelCap(int petHouseLevel, int petMaxLevel)
        {
            int cap = BaseLevelCap + Math.Max(0, petHouseLevel) * LevelsPerPetHouseLevel;
            return Math.Max(1, Math.Min(Math.Max(1, petMaxLevel), cap));
        }

        /// <summary>Lowest Pet House level whose cap allows <paramref name="targetLevel"/>.</summary>
        public static int GetRequiredPetHouseLevel(int targetLevel)
        {
            if (targetLevel <= BaseLevelCap) return 0;
            return (targetLevel - BaseLevelCap + LevelsPerPetHouseLevel - 1) / LevelsPerPetHouseLevel;
        }

        public static int GetUpgradeCost(int currentLevel, int baseCost, float exponent)
        {
            if (baseCost <= 0) return 0;
            int level = Math.Max(1, currentLevel);
            double raw = baseCost * Math.Pow(level, Math.Max(0f, exponent));
            long rounded = (long)Math.Round(raw / CostRounding, MidpointRounding.AwayFromZero) * CostRounding;
            return (int)Math.Min(int.MaxValue, Math.Max(CostRounding, rounded));
        }

        /// <param name="bypassSlotLimit">Store grants ignore the slot limit so paid pets are never blocked.</param>
        public static PetActionResult CheckUnlock(bool alreadyOwned, int ownedCount, int petHouseLevel, int requiredPetHouseLevel,
            int balance, int cost, bool bypassSlotLimit = false)
        {
            if (alreadyOwned) return PetActionResult.AlreadyUnlocked;
            if (petHouseLevel < requiredPetHouseLevel) return PetActionResult.PetHouseLevelTooLow;
            if (!bypassSlotLimit && ownedCount >= GetMaxOwnedPets(petHouseLevel)) return PetActionResult.PetSlotsFull;
            if (balance < cost) return PetActionResult.NotEnoughCurrency;
            return PetActionResult.Success;
        }

        public static PetActionResult CheckUpgrade(bool owned, int currentLevel, int levelCap, int maxLevel, int balance, int cost)
        {
            if (!owned) return PetActionResult.NotUnlocked;
            if (currentLevel >= maxLevel) return PetActionResult.MaxLevel;
            if (currentLevel >= levelCap) return PetActionResult.LevelCapReached;
            if (balance < cost) return PetActionResult.NotEnoughCurrency;
            return PetActionResult.Success;
        }

        public static PetActionResult CheckBuyEquipment(bool alreadyOwned, int petHouseLevel, int requiredPetHouseLevel, int balance, int cost)
        {
            if (alreadyOwned) return PetActionResult.AlreadyOwned;
            if (petHouseLevel < requiredPetHouseLevel) return PetActionResult.PetHouseLevelTooLow;
            if (balance < cost) return PetActionResult.NotEnoughCurrency;
            return PetActionResult.Success;
        }
    }
}
