using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>Which equipment item a pet has equipped (EPIC 12 "Pet equipment").</summary>
    [Serializable]
    public class PetEquipmentSlot
    {
        public string PetId;
        public string ItemId;
    }

    /// <summary>
    /// Unlocked pets, their levels, the active companion and pet gear (EPIC 12). Owned by the
    /// Pet system. Each pet has one gear slot; each owned gear item can be worn by one pet at a time.
    /// </summary>
    [Serializable]
    public class PetSaveData
    {
        /// <summary>Pet definition id → pet level. Presence = unlocked.</summary>
        public List<IdLevelEntry> Owned = new();
        public string ActivePetId;
        public List<PetEquipmentSlot> Equipment = new();
        /// <summary>Pet gear ids the player has bought (each item is owned once).</summary>
        public List<string> OwnedEquipmentIds = new();

        public bool IsUnlocked(string petId) => Owned.ContainsId(petId);
        public int GetLevel(string petId) => Owned.GetLevel(petId);

        /// <summary>Repairs lists a hand-edited / cloud-merged save may have nulled.</summary>
        public void EnsureInitialized()
        {
            Owned ??= new List<IdLevelEntry>();
            Equipment ??= new List<PetEquipmentSlot>();
            OwnedEquipmentIds ??= new List<string>();
        }

        public bool OwnsEquipment(string itemId) =>
            !string.IsNullOrEmpty(itemId) && OwnedEquipmentIds != null && OwnedEquipmentIds.Contains(itemId);

        /// <summary>Adds the gear item to the owned list. Returns false if it was already owned.</summary>
        public bool AddEquipment(string itemId)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(itemId) || OwnedEquipmentIds.Contains(itemId)) return false;
            OwnedEquipmentIds.Add(itemId);
            return true;
        }

        /// <summary>The gear id worn by <paramref name="petId"/>, or null.</summary>
        public string GetEquippedItem(string petId)
        {
            if (Equipment == null || string.IsNullOrEmpty(petId)) return null;
            foreach (var slot in Equipment)
                if (slot != null && slot.PetId == petId) return string.IsNullOrEmpty(slot.ItemId) ? null : slot.ItemId;
            return null;
        }

        /// <summary>The pet currently wearing <paramref name="itemId"/>, or null.</summary>
        public string GetPetUsingItem(string itemId)
        {
            if (Equipment == null || string.IsNullOrEmpty(itemId)) return null;
            foreach (var slot in Equipment)
                if (slot != null && slot.ItemId == itemId) return slot.PetId;
            return null;
        }

        /// <summary>
        /// Puts <paramref name="itemId"/> on <paramref name="petId"/> (null/empty = unequip). An item
        /// worn by another pet moves over — the same item is never on two pets.
        /// </summary>
        public void SetEquippedItem(string petId, string itemId)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(petId)) return;

            for (int i = Equipment.Count - 1; i >= 0; i--)
            {
                var slot = Equipment[i];
                if (slot == null || slot.PetId == petId ||
                    (!string.IsNullOrEmpty(itemId) && slot.ItemId == itemId))
                {
                    Equipment.RemoveAt(i);
                }
            }

            if (!string.IsNullOrEmpty(itemId))
                Equipment.Add(new PetEquipmentSlot { PetId = petId, ItemId = itemId });
        }
    }
}
