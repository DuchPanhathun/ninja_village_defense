using System;
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
    /// defaults (own + equip the starter Kunai), equipment slot limits, and choosing spare equipment
    /// as Forge crafting materials. No Unity objects, so it's covered by EditMode tests.
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

        /// <summary>Pieces of this equipment not currently worn (the equipped one is never consumed).</summary>
        public static int SpareCount(InventorySaveData inv, string equipmentId)
        {
            if (inv == null) return 0;
            int owned = inv.GetEquipmentCount(equipmentId);
            return Math.Max(0, owned - (inv.IsEquipmentEquipped(equipmentId) ? 1 : 0));
        }

        /// <summary>
        /// Picks <paramref name="count"/> spare equipment pieces of at least <paramref name="minRarity"/>,
        /// cheapest rarity first, as crafting materials. <paramref name="rarityOf"/> maps an equipment id
        /// to its rarity index (-1 = unknown, never picked). <paramref name="picked"/> gets one id per piece.
        /// Returns true when enough pieces were found.
        /// </summary>
        public static bool PickMaterials(InventorySaveData inv, Func<string, int> rarityOf, int minRarity, int count, List<string> picked)
        {
            picked.Clear();
            if (count <= 0) return true;
            if (inv == null || rarityOf == null) return false;

            var candidates = new List<(string id, int rarity, int spare)>();
            foreach (var stack in inv.Equipment)
            {
                if (stack == null || string.IsNullOrEmpty(stack.Id)) continue;
                int rarity = rarityOf(stack.Id);
                if (rarity < 0 || rarity < minRarity) continue;
                int spare = SpareCount(inv, stack.Id);
                if (spare > 0) candidates.Add((stack.Id, rarity, spare));
            }

            candidates.Sort((a, b) =>
            {
                int byRarity = a.rarity.CompareTo(b.rarity);
                return byRarity != 0 ? byRarity : string.CompareOrdinal(a.id, b.id);
            });

            foreach (var candidate in candidates)
            {
                for (int i = 0; i < candidate.spare && picked.Count < count; i++)
                    picked.Add(candidate.id);
                if (picked.Count >= count) break;
            }
            return picked.Count >= count;
        }

        /// <summary>Removes every id in <paramref name="picked"/> (one piece each) from the inventory.</summary>
        public static void ConsumeMaterials(InventorySaveData inv, List<string> picked)
        {
            if (inv == null || picked == null) return;
            foreach (var id in picked)
                inv.RemoveEquipment(id, 1);
        }
    }
}
