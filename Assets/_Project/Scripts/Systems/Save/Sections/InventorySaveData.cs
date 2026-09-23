using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>A stack of one equipment definition the player owns.</summary>
    [Serializable]
    public class OwnedEquipment
    {
        public string Id;
        public int Count;
    }

    /// <summary>
    /// Persistent gear between runs (EPIC 16 "Inventory save"): owned weapons with
    /// their Forge level, owned equipment stacks, and what's equipped for the next run.
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        /// <summary>Weapon definition id → Forge level (1..RuntimeWeapon.MaxLevel).</summary>
        public List<IdLevelEntry> Weapons = new();
        public string EquippedWeaponId;

        /// <summary>
        /// Weapon definition id → Forge tier (0 = Iron, 1 = Steel, 2 = Golden, 3 = Legendary; see
        /// EconomyConfig). Missing = tier 0. Kept separate from <see cref="Weapons"/> so the level list keeps
        /// its simple "id → level" meaning for other readers.
        /// </summary>
        public List<IdLevelEntry> WeaponTiers = new();

        public List<OwnedEquipment> Equipment = new();
        /// <summary>Equipment ids applied at the start of every run (slot count gated by the Forge/Castle).</summary>
        public List<string> EquippedEquipmentIds = new();

        public bool OwnsWeapon(string weaponId) => Weapons.ContainsId(weaponId);
        public int GetWeaponLevel(string weaponId) => Weapons.GetLevel(weaponId);

        /// <summary>Adds the weapon at level 1 if not owned. Returns true if newly added.</summary>
        public bool AddWeapon(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId) || Weapons.ContainsId(weaponId)) return false;
            Weapons.SetLevel(weaponId, 1);
            return true;
        }

        public int GetWeaponTier(string weaponId) => WeaponTiers != null ? WeaponTiers.GetLevel(weaponId) : 0;

        public void SetWeaponTier(string weaponId, int tier)
        {
            if (string.IsNullOrEmpty(weaponId)) return;
            WeaponTiers ??= new List<IdLevelEntry>();
            WeaponTiers.SetLevel(weaponId, tier < 0 ? 0 : tier);
        }

        public bool IsEquipmentEquipped(string equipmentId) =>
            EquippedEquipmentIds != null && EquippedEquipmentIds.Contains(equipmentId);

        public int GetEquipmentCount(string equipmentId)
        {
            foreach (var e in Equipment)
                if (e.Id == equipmentId) return e.Count;
            return 0;
        }

        public void AddEquipment(string equipmentId, int count = 1)
        {
            if (string.IsNullOrEmpty(equipmentId) || count <= 0) return;
            foreach (var e in Equipment)
            {
                if (e.Id != equipmentId) continue;
                e.Count += count;
                return;
            }
            Equipment.Add(new OwnedEquipment { Id = equipmentId, Count = count });
        }

        /// <summary>Removes up to <paramref name="count"/>; unequips when the stack hits zero.</summary>
        public bool RemoveEquipment(string equipmentId, int count = 1)
        {
            for (int i = 0; i < Equipment.Count; i++)
            {
                var e = Equipment[i];
                if (e.Id != equipmentId) continue;
                if (e.Count < count) return false;

                e.Count -= count;
                if (e.Count <= 0)
                {
                    Equipment.RemoveAt(i);
                    EquippedEquipmentIds.Remove(equipmentId);
                }
                return true;
            }
            return false;
        }
    }
}
