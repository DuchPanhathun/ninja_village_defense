using System.Collections.Generic;
using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Data;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Pets;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Pets
{
    /// <summary>
    /// Pet management (EPIC 12 + Pet House "Pet management"): unlocking companions, leveling them,
    /// buying/equipping pet gear and choosing the one that joins the next run. How many pets can be
    /// owned and how far they can level is gated by the Pet House (<see cref="BuildingIds.PetHouse"/>).
    /// Pure rules live in <see cref="PetRules"/>; this wires them to the catalogs, wallet and save.
    /// </summary>
    public static class PetService
    {
        public static PetCatalog Catalog => CatalogLoader.Load<PetCatalog>();
        public static PetEquipmentCatalog EquipmentCatalog => CatalogLoader.Load<PetEquipmentCatalog>();

        public static int PetHouseLevel => SaveService.Data.Village.GetBuildingLevel(BuildingIds.PetHouse);
        public static int MaxOwnedPets => PetRules.GetMaxOwnedPets(PetHouseLevel);

        private static PetSaveData Data
        {
            get
            {
                var pets = SaveService.Data.Pets;
                pets.EnsureInitialized();
                return pets;
            }
        }

        public static List<PetDefinition> GetSortedPets()
        {
            var list = new List<PetDefinition>();
            var catalog = Catalog;
            if (catalog == null) return list;
            foreach (var pet in catalog.All)
                if (pet != null) list.Add(pet);
            list.Sort((a, b) => a.SortOrder != b.SortOrder ? a.SortOrder.CompareTo(b.SortOrder) : string.CompareOrdinal(a.Id, b.Id));
            return list;
        }

        public static List<PetEquipmentDefinition> GetSortedEquipment()
        {
            var list = new List<PetEquipmentDefinition>();
            var catalog = EquipmentCatalog;
            if (catalog == null) return list;
            foreach (var item in catalog.All)
                if (item != null) list.Add(item);
            list.Sort((a, b) => a.SortOrder != b.SortOrder ? a.SortOrder.CompareTo(b.SortOrder) : string.CompareOrdinal(a.Id, b.Id));
            return list;
        }

        public static PetDefinition Get(string petId)
        {
            var catalog = Catalog;
            return catalog != null ? catalog.Get(petId) : null;
        }

        public static PetEquipmentDefinition GetItem(string itemId)
        {
            var catalog = EquipmentCatalog;
            return catalog != null ? catalog.Get(itemId) : null;
        }

        public static int OwnedCount => Data.Owned.Count;
        public static bool IsUnlocked(PetDefinition pet) => pet != null && Data.IsUnlocked(pet.Id);
        public static int GetLevel(PetDefinition pet) => pet != null ? UnityEngine.Mathf.Max(1, Data.GetLevel(pet.Id)) : 0;
        public static int GetLevelCap(PetDefinition pet) => pet != null ? PetRules.GetLevelCap(PetHouseLevel, pet.MaxLevel) : 0;
        public static int GetUpgradeCost(PetDefinition pet) =>
            pet != null ? PetRules.GetUpgradeCost(GetLevel(pet), pet.UpgradeCostBase, pet.UpgradeCostExponent) : 0;

        public static PetDefinition GetActive() => Get(Data.ActivePetId);
        public static bool IsActive(PetDefinition pet) => pet != null && Data.ActivePetId == pet.Id;

        /// <summary>The pet's equipped item bonus (empty when nothing is equipped).</summary>
        public static PetEquipmentBonus GetGear(string petId)
        {
            var item = GetItem(Data.GetEquippedItem(petId));
            return item != null ? item.Bonus : default;
        }

        public static PetEquipmentDefinition GetEquippedItem(PetDefinition pet) =>
            pet != null ? GetItem(Data.GetEquippedItem(pet.Id)) : null;

        public static bool OwnsItem(PetEquipmentDefinition item) => item != null && Data.OwnsEquipment(item.Id);

        // ------------------------------------------------------------------ unlock

        public static PetActionResult CheckUnlock(PetDefinition pet)
        {
            if (pet == null) return PetActionResult.InvalidPet;
            return PetRules.CheckUnlock(Data.IsUnlocked(pet.Id), OwnedCount, PetHouseLevel, pet.RequiredPetHouseLevel,
                CurrencyService.Balance(pet.UnlockCurrency), pet.UnlockCost);
        }

        public static PetActionResult TryUnlock(PetDefinition pet)
        {
            var result = CheckUnlock(pet);
            if (result != PetActionResult.Success) return result;
            if (!CurrencyService.TrySpend(new Price(pet.UnlockCurrency, pet.UnlockCost), pet.Id))
                return PetActionResult.NotEnoughCurrency;

            GrantInternal(pet.Id);
            return PetActionResult.Success;
        }

        /// <summary>Unlocks without payment or slot checks (store bundles, rewards). Returns false if already owned.</summary>
        public static bool Grant(string petId)
        {
            if (string.IsNullOrEmpty(petId) || Data.IsUnlocked(petId)) return false;
            GrantInternal(petId);
            return true;
        }

        private static void GrantInternal(string petId)
        {
            Data.Owned.SetLevel(petId, 1);
            if (string.IsNullOrEmpty(Data.ActivePetId)) Data.ActivePetId = petId;
            SaveService.SaveNow();
            Progress.Report(ProgressStatIds.PetUnlocked, 1, petId);
            EventBus<PetUnlockedEvent>.Raise(new PetUnlockedEvent(petId));
            Sfx.Play(AudioCueIds.UiPurchase);
        }

        // ------------------------------------------------------------------ level

        public static PetActionResult CheckUpgrade(PetDefinition pet)
        {
            if (pet == null) return PetActionResult.InvalidPet;
            return PetRules.CheckUpgrade(IsUnlocked(pet), GetLevel(pet), GetLevelCap(pet), pet.MaxLevel,
                CurrencyService.Balance(CurrencyType.Coins), GetUpgradeCost(pet));
        }

        public static PetActionResult TryUpgrade(PetDefinition pet)
        {
            var result = CheckUpgrade(pet);
            if (result != PetActionResult.Success) return result;
            if (!CurrencyService.TrySpend(Price.Coins(GetUpgradeCost(pet)), pet.Id))
                return PetActionResult.NotEnoughCurrency;

            int newLevel = GetLevel(pet) + 1;
            Data.Owned.SetLevel(pet.Id, newLevel);
            SaveService.SaveNow();
            Progress.Report(ProgressStatIds.PetUpgraded, 1, pet.Id);
            EventBus<PetLevelChangedEvent>.Raise(new PetLevelChangedEvent(pet.Id, newLevel));
            Sfx.Play(AudioCueIds.UiUpgrade);
            return PetActionResult.Success;
        }

        // ------------------------------------------------------------------ active pet

        public static PetActionResult TrySetActive(PetDefinition pet)
        {
            if (pet == null) return PetActionResult.InvalidPet;
            if (!IsUnlocked(pet)) return PetActionResult.NotUnlocked;
            Data.ActivePetId = pet.Id;
            SaveService.SaveNow();
            EventBus<ActivePetChangedEvent>.Raise(new ActivePetChangedEvent(pet.Id));
            return PetActionResult.Success;
        }

        /// <summary>Leave the pet at home for the next run.</summary>
        public static void ClearActive()
        {
            Data.ActivePetId = null;
            SaveService.SaveNow();
            EventBus<ActivePetChangedEvent>.Raise(new ActivePetChangedEvent(null));
        }

        // ------------------------------------------------------------------ equipment

        public static PetActionResult CheckBuyItem(PetEquipmentDefinition item)
        {
            if (item == null) return PetActionResult.InvalidItem;
            return PetRules.CheckBuyEquipment(Data.OwnsEquipment(item.Id), PetHouseLevel, item.RequiredPetHouseLevel,
                CurrencyService.Balance(item.CostCurrency), item.Cost);
        }

        public static PetActionResult TryBuyItem(PetEquipmentDefinition item)
        {
            var result = CheckBuyItem(item);
            if (result != PetActionResult.Success) return result;
            if (!CurrencyService.TrySpend(new Price(item.CostCurrency, item.Cost), item.Id))
                return PetActionResult.NotEnoughCurrency;

            Data.AddEquipment(item.Id);
            SaveService.SaveNow();
            Sfx.Play(AudioCueIds.UiPurchase);
            return PetActionResult.Success;
        }

        /// <summary>Equips <paramref name="item"/> on <paramref name="pet"/> (moving it off another pet), or unequips when item is null.</summary>
        public static PetActionResult TryEquip(PetDefinition pet, PetEquipmentDefinition item)
        {
            if (pet == null) return PetActionResult.InvalidPet;
            if (!IsUnlocked(pet)) return PetActionResult.NotUnlocked;
            if (item != null && !Data.OwnsEquipment(item.Id)) return PetActionResult.ItemNotOwned;

            string itemId = item != null ? item.Id : null;
            Data.SetEquippedItem(pet.Id, itemId);
            SaveService.SaveNow();
            EventBus<PetEquipmentChangedEvent>.Raise(new PetEquipmentChangedEvent(pet.Id, itemId));
            return PetActionResult.Success;
        }

        public static string Describe(PetActionResult result, PetDefinition pet = null)
        {
            switch (result)
            {
                case PetActionResult.Success: return string.Empty;
                case PetActionResult.InvalidPet: return "Unknown pet";
                case PetActionResult.InvalidItem: return "Unknown item";
                case PetActionResult.AlreadyUnlocked: return "Already unlocked";
                case PetActionResult.AlreadyOwned: return "Already owned";
                case PetActionResult.NotUnlocked: return "Unlock this pet first";
                case PetActionResult.ItemNotOwned: return "Buy this item first";
                case PetActionResult.NotEnoughCurrency: return "Not enough currency";
                case PetActionResult.PetHouseLevelTooLow:
                    return pet != null ? $"Requires Pet House Lv {pet.RequiredPetHouseLevel}" : "Upgrade the Pet House";
                case PetActionResult.PetSlotsFull: return $"Pet House is full ({MaxOwnedPets} pets) — upgrade it for more room";
                case PetActionResult.LevelCapReached:
                    return pet != null
                        ? $"Upgrade the Pet House to Lv {PetRules.GetRequiredPetHouseLevel(GetLevel(pet) + 1)}"
                        : "Upgrade the Pet House";
                case PetActionResult.MaxLevel: return "Max level";
                default: return "Unavailable";
            }
        }
    }
}
