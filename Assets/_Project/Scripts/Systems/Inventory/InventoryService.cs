using System.Collections.Generic;
using NinjaVillage.Core;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Loot;
using NinjaVillage.Gameplay.Weapons;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using UnityEngine;

namespace NinjaVillage.Systems.Inventory
{
    /// <summary>
    /// Persistent inventory between runs (EPIC 16 "Inventory save"): which weapons and equipment the
    /// player owns and what is equipped for the next run. Wraps the plain rules in
    /// <see cref="InventoryRules"/> with catalogs, saving and change events so every screen (Inventory,
    /// Forge, Market) and every source of loot (pickups, chests, market) goes through one path.
    /// Forge operations live in <see cref="ForgeService"/>.
    /// </summary>
    public static class InventoryService
    {
        /// <summary>Id of the weapon every fresh save starts with (Data/Weapons/Kunai.asset).</summary>
        public const string StarterWeaponId = "Kunai";

        public static WeaponCatalog Weapons => CatalogCache<WeaponCatalog>.Get();
        public static EquipmentCatalog EquipmentDefs => CatalogCache<EquipmentCatalog>.Get();

        public static InventorySaveData Data
        {
            get
            {
                var inv = SaveService.Data.Inventory;
                if (InventoryRules.EnsureDefaults(inv, StarterWeaponId)) SaveService.MarkDirty();
                return inv;
            }
        }

        /// <summary>Castle effect raises this from 2 to 5 (data-defined on the Castle building).</summary>
        public static int EquipmentSlots
        {
            get
            {
                int fromCastle = VillageRules.FloorEffect(VillageService.GetEffect(BuildingIds.Castle, InventoryRules.DefaultEquipmentSlots));
                return Mathf.Max(InventoryRules.DefaultEquipmentSlots, fromCastle);
            }
        }

        // ------------------------------------------------------------------ lookups

        public static WeaponDefinition GetWeapon(string weaponId)
        {
            var catalog = Weapons;
            return catalog != null ? catalog.Get(weaponId) : null;
        }

        public static EquipmentDefinition GetEquipment(string equipmentId)
        {
            var catalog = EquipmentDefs;
            return catalog != null ? catalog.Get(equipmentId) : null;
        }

        public static WeaponDefinition EquippedWeapon => GetWeapon(Data.EquippedWeaponId);

        /// <summary>Rarity index of an equipment id, -1 when unknown (used for crafting materials).</summary>
        public static int RarityIndexOf(string equipmentId)
        {
            var def = GetEquipment(equipmentId);
            return def != null ? (int)def.Rarity : -1;
        }

        public static bool HasEquipmentOfRarity(Rarity rarity)
        {
            var catalog = EquipmentDefs;
            if (catalog == null) return false;
            foreach (var def in catalog.All)
                if (def != null && def.Rarity == rarity && !string.IsNullOrEmpty(def.Id)) return true;
            return false;
        }

        private static readonly List<EquipmentDefinition> Candidates = new();

        /// <summary>A random catalog piece of exactly <paramref name="rarity"/>, or null if the catalog has none.</summary>
        public static EquipmentDefinition RandomEquipmentOfRarity(Rarity rarity)
        {
            var catalog = EquipmentDefs;
            if (catalog == null) return null;

            Candidates.Clear();
            foreach (var def in catalog.All)
                if (def != null && def.Rarity == rarity && !string.IsNullOrEmpty(def.Id)) Candidates.Add(def);
            return Candidates.Count > 0 ? Candidates[Random.Range(0, Candidates.Count)] : null;
        }

        // ------------------------------------------------------------------ mutations

        /// <summary>Adds equipment (run pickups, chests, market). Batched save; reports EquipmentCollected.</summary>
        public static void AddEquipment(string equipmentId, int count = 1)
        {
            if (string.IsNullOrEmpty(equipmentId) || count <= 0) return;
            Data.AddEquipment(equipmentId, count);
            SaveService.MarkDirty();
            Progress.Report(ProgressStatIds.EquipmentCollected, count, equipmentId);
            Raise(InventoryChangeKind.EquipmentAdded, equipmentId);
        }

        /// <summary>Adds a weapon at level 1 if it isn't owned yet. Returns true when newly added.</summary>
        public static bool AddWeapon(string weaponId)
        {
            if (!Data.AddWeapon(weaponId)) return false;
            SaveService.SaveNow();
            Raise(InventoryChangeKind.WeaponAdded, weaponId);
            return true;
        }

        public static bool EquipWeapon(string weaponId)
        {
            if (!InventoryRules.EquipWeapon(Data, weaponId)) return false;
            SaveService.SaveNow();
            Raise(InventoryChangeKind.WeaponEquipped, weaponId);
            return true;
        }

        public static EquipResult EquipEquipment(string equipmentId)
        {
            var result = InventoryRules.Equip(Data, equipmentId, EquipmentSlots);
            if (result == EquipResult.Ok)
            {
                SaveService.SaveNow();
                Raise(InventoryChangeKind.EquipmentEquipped, equipmentId);
            }
            return result;
        }

        public static EquipResult UnequipEquipment(string equipmentId)
        {
            var result = InventoryRules.Unequip(Data, equipmentId);
            if (result == EquipResult.Ok)
            {
                SaveService.SaveNow();
                Raise(InventoryChangeKind.EquipmentUnequipped, equipmentId);
            }
            return result;
        }

        public static string DescribeEquipResult(EquipResult result)
        {
            switch (result)
            {
                case EquipResult.Ok: return string.Empty;
                case EquipResult.NotOwned: return "You don't own this";
                case EquipResult.AlreadyEquipped: return "Already equipped";
                case EquipResult.NoFreeSlot: return $"All {EquipmentSlots} slots are full — upgrade the Castle for more";
                case EquipResult.NotEquipped: return "Not equipped";
                default: return "Unavailable";
            }
        }

        internal static void Raise(InventoryChangeKind kind, string itemId) =>
            EventBus<InventoryChangedEvent>.Raise(new InventoryChangedEvent(kind, itemId));
    }
}
