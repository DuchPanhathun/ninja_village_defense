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
    /// player owns (copies by grade — see <see cref="GradeRules"/>) and what is equipped for the next run. Wraps the plain rules in
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
                if (inv.GradeVersion < 1 && MigrateToGrades(inv)) SaveService.MarkDirty();
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

        // ------------------------------------------------------------------ grades

        public static ItemGrade NativeGrade(EquipmentDefinition def) => def != null ? GradeRules.Native(def.Rarity, def.IsSpecial) : ItemGrade.Common;
        public static ItemGrade NativeGrade(WeaponDefinition def) => def != null ? GradeRules.Native(def.Rarity, def.IsSpecial) : ItemGrade.Common;

        /// <summary>The grade an equipped piece uses: the best one owned (its native grade when none is owned).</summary>
        public static ItemGrade GearGrade(EquipmentDefinition def)
        {
            if (def == null) return ItemGrade.Common;
            int best = Data.BestEquipmentGrade(def.Id);
            return best >= 0 ? GradeRules.Clamp(best) : NativeGrade(def);
        }

        /// <summary>The grade a weapon fights at: the best copy owned (Common when none).</summary>
        public static ItemGrade WeaponGrade(string weaponId)
        {
            int best = Data.BestWeaponGrade(weaponId);
            return best >= 0 ? GradeRules.Clamp(best) : NativeGrade(GetWeapon(weaponId));
        }

        /// <summary>
        /// Saves from before grades: gear stacks take their item's native grade (a Dragon Scale stays Legendary),
        /// and every owned weapon gets one copy at the better of its native grade and its old Forge tier
        /// (Steel → Rare, Golden → Epic, Legendary → Legendary), so nothing gets weaker.
        /// </summary>
        private static bool MigrateToGrades(InventorySaveData inv)
        {
            foreach (var stack in inv.Equipment)
                if (stack != null) stack.Grade = (int)NativeGrade(GetEquipment(stack.Id));
            foreach (var weapon in inv.Weapons)
            {
                if (weapon == null || string.IsNullOrEmpty(weapon.Id) || inv.BestWeaponGrade(weapon.Id) >= 0) continue;
                int tier = inv.GetWeaponTier(weapon.Id);
                var fromTier = tier switch { 1 => ItemGrade.Rare, 2 => ItemGrade.Epic, >= 3 => ItemGrade.Legendary, _ => ItemGrade.Common };
                var native = NativeGrade(GetWeapon(weapon.Id));
                inv.AddWeaponCopies(weapon.Id, 1, (int)(fromTier > native ? fromTier : native));
            }
            inv.GradeVersion = 1;
            return true;
        }

        public static bool HasEquipmentOfRarity(Rarity rarity)
        {
            var catalog = EquipmentDefs;
            if (catalog == null) return false;
            foreach (var def in catalog.All)
                if (def != null && def.Rarity == rarity && !def.IsSpecial && !string.IsNullOrEmpty(def.Id)) return true;
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
                if (def != null && def.Rarity == rarity && !def.IsSpecial && !string.IsNullOrEmpty(def.Id)) Candidates.Add(def);
            return Candidates.Count > 0 ? Candidates[Random.Range(0, Candidates.Count)] : null;
        }

        // ------------------------------------------------------------------ mutations

        /// <summary>
        /// Adds equipment (run pickups, chests, market) at <paramref name="grade"/> — its native grade when not given.
        /// Batched save; reports EquipmentCollected.
        /// </summary>
        public static void AddEquipment(string equipmentId, int count = 1, ItemGrade? grade = null)
        {
            if (string.IsNullOrEmpty(equipmentId) || count <= 0) return;
            Data.AddEquipment(equipmentId, count, (int)(grade ?? NativeGrade(GetEquipment(equipmentId))));
            SaveService.MarkDirty();
            Progress.Report(ProgressStatIds.EquipmentCollected, count, equipmentId);
            Raise(InventoryChangeKind.EquipmentAdded, equipmentId);
        }

        /// <summary>Adds a weapon at level 1 (one copy at its native grade) if it isn't owned yet. Returns true when newly added.</summary>
        public static bool AddWeapon(string weaponId)
        {
            if (!Data.AddWeapon(weaponId, (int)NativeGrade(GetWeapon(weaponId)))) return false;
            SaveService.SaveNow();
            Raise(InventoryChangeKind.WeaponAdded, weaponId);
            return true;
        }

        /// <summary>
        /// Another copy of a weapon (Forge, battle chests) at its native grade — for merging. A weapon not owned yet
        /// is unlocked instead. Batched save.
        /// </summary>
        public static void AddWeaponCopy(string weaponId, int count = 1)
        {
            var def = GetWeapon(weaponId);
            if (def == null || count <= 0) return;
            if (!Data.OwnsWeapon(weaponId))
            {
                AddWeapon(weaponId);
                count--;
            }
            if (count <= 0) return;
            Data.AddWeaponCopies(weaponId, count, (int)NativeGrade(def));
            SaveService.MarkDirty();
            Raise(InventoryChangeKind.WeaponAdded, weaponId);
        }

        /// <summary>
        /// A weapon copy at a given grade (supply crates): unlocks the weapon (level 1) if it's new. Returns true
        /// when the weapon was new. Batched save.
        /// </summary>
        public static bool GrantWeapon(string weaponId, ItemGrade grade)
        {
            if (GetWeapon(weaponId) == null) return false;
            var inv = Data;
            bool isNew = !inv.OwnsWeapon(weaponId);
            if (isNew) inv.Weapons.SetLevel(weaponId, 1);
            inv.AddWeaponCopies(weaponId, 1, (int)grade);
            SaveService.MarkDirty();
            Raise(InventoryChangeKind.WeaponAdded, weaponId);
            return isNew;
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
