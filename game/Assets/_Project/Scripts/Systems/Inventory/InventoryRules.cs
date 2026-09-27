using System.Collections.Generic;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Inventory
{
    public enum EquipResult
    {
        Ok,
        InvalidId,
        NotOwned,
        AlreadyEquipped,
        NoFreeSlot,
        NotEquipped
    }

    /// <summary>
    /// Persistent-inventory rules on the plain save section (EPIC 16 "Inventory save"): fresh-save
    /// defaults (own + equip the starter Kunai) and equipment slot limits. Grades and merging are in
    /// <see cref="GradeRules"/> / <see cref="MergeService"/>. No Unity objects, so it's covered by EditMode tests.
    /// </summary>
    public static class InventoryRules
    {
        /// <summary>Slots when the Castle definition is missing; the Castle's effect raises it (2 → 5).</summary>
        public const int DefaultEquipmentSlots = 2;

        /// <summary>Starter weapon, repairs the equipped weapon and drops stale equipped ids. Returns true if anything changed.</summary>
        public static bool EnsureDefaults(InventorySaveData inv, string starterWeaponId)
        {
            if (inv == null) return false;
            bool changed = false;

            inv.Weapons ??= new List<IdLevelEntry>();
            inv.WeaponTiers ??= new List<IdLevelEntry>();
            inv.WeaponCopies ??= new List<OwnedEquipment>();
            inv.Equipment ??= new List<OwnedEquipment>();
            inv.EquippedEquipmentIds ??= new List<string>();

            if (inv.Weapons.Count == 0 && !string.IsNullOrEmpty(starterWeaponId))
                changed |= inv.AddWeapon(starterWeaponId);

            if (string.IsNullOrEmpty(inv.EquippedWeaponId) || !inv.OwnsWeapon(inv.EquippedWeaponId))
            {
                string pick = inv.OwnsWeapon(starterWeaponId) ? starterWeaponId : null;
                if (pick == null)
                {
                    foreach (var weapon in inv.Weapons)
                    {
                        if (weapon == null || string.IsNullOrEmpty(weapon.Id)) continue;
                        pick = weapon.Id;
                        break;
                    }
                }
                if (pick != inv.EquippedWeaponId)
                {
                    inv.EquippedWeaponId = pick;
                    changed = true;
                }
            }

            // Remove equipped ids that are empty, no longer owned, or duplicated.
            for (int i = inv.EquippedEquipmentIds.Count - 1; i >= 0; i--)
            {
                string id = inv.EquippedEquipmentIds[i];
                if (string.IsNullOrEmpty(id) || inv.GetEquipmentCount(id) <= 0 || inv.EquippedEquipmentIds.IndexOf(id) != i)
                {
                    inv.EquippedEquipmentIds.RemoveAt(i);
                    changed = true;
                }
            }
            return changed;
        }

        public static EquipResult Equip(InventorySaveData inv, string equipmentId, int slotCount)
        {
            if (inv == null || string.IsNullOrEmpty(equipmentId)) return EquipResult.InvalidId;
            if (inv.GetEquipmentCount(equipmentId) <= 0) return EquipResult.NotOwned;
            if (inv.IsEquipmentEquipped(equipmentId)) return EquipResult.AlreadyEquipped;
            if (inv.EquippedEquipmentIds.Count >= slotCount) return EquipResult.NoFreeSlot;

            inv.EquippedEquipmentIds.Add(equipmentId);
            return EquipResult.Ok;
        }

        public static EquipResult Unequip(InventorySaveData inv, string equipmentId)
        {
            if (inv == null || string.IsNullOrEmpty(equipmentId)) return EquipResult.InvalidId;
            return inv.EquippedEquipmentIds.Remove(equipmentId) ? EquipResult.Ok : EquipResult.NotEquipped;
        }

        public static bool EquipWeapon(InventorySaveData inv, string weaponId)
        {
            if (inv == null || !inv.OwnsWeapon(weaponId)) return false;
            inv.EquippedWeaponId = weaponId;
            return true;
        }
    }
}
