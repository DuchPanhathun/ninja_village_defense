using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>A stack of copies of one weapon or equipment definition at one grade (0 = Common .. 4 = Legendary).</summary>
    [Serializable]
    public class OwnedEquipment
    {
        public string Id;
        public int Count;
        public int Grade;
    }

    /// <summary>
    /// Persistent gear between runs (EPIC 16 "Inventory save"): owned weapons with their Forge level and their
    /// copies by grade, owned equipment stacks by grade, and what's equipped for the next run. An equipped item
    /// (weapon or gear) always uses the best grade of it you own.
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        /// <summary>Weapon definition id → Forge level (1..RuntimeWeapon.MaxLevel). Being listed here means owned.</summary>
        public List<IdLevelEntry> Weapons = new();
        public string EquippedWeaponId;

        /// <summary>Copies of each owned weapon by grade (merged 3 → 1 of the next grade).</summary>
        public List<OwnedEquipment> WeaponCopies = new();

        /// <summary>
        /// Legacy (before grades): weapon id → Forge tier (0 Iron, 1 Steel, 2 Golden, 3 Legendary). Only read once, to
        /// give old saves weapon copies of a matching grade.
        /// </summary>
        public List<IdLevelEntry> WeaponTiers = new();

        public List<OwnedEquipment> Equipment = new();
        /// <summary>Equipment ids applied at the start of every run (slot count gated by the Castle).</summary>
        public List<string> EquippedEquipmentIds = new();

        /// <summary>0 = saved before grades existed (stacks carry no grade yet); set to 1 once migrated.</summary>
        public int GradeVersion;

        public bool OwnsWeapon(string weaponId) => Weapons.ContainsId(weaponId);
        public int GetWeaponLevel(string weaponId) => Weapons.GetLevel(weaponId);

        /// <summary>Adds the weapon at level 1 if not owned (with one copy at <paramref name="grade"/>). Returns true if newly added.</summary>
        public bool AddWeapon(string weaponId, int grade = 0)
        {
            if (string.IsNullOrEmpty(weaponId) || Weapons.ContainsId(weaponId)) return false;
            Weapons.SetLevel(weaponId, 1);
            AddCopies(WeaponCopies, weaponId, 1, grade);
            return true;
        }

        public int GetWeaponTier(string weaponId) => WeaponTiers != null ? WeaponTiers.GetLevel(weaponId) : 0;

        // ------------------------------------------------------------------ weapon copies

        public int GetWeaponCopies(string weaponId, int grade) => Count(WeaponCopies, weaponId, grade);
        public void AddWeaponCopies(string weaponId, int count, int grade) => AddCopies(WeaponCopies, weaponId, count, grade);
        public bool RemoveWeaponCopies(string weaponId, int count, int grade) => RemoveCopies(WeaponCopies, weaponId, count, grade);
        /// <summary>Best grade of this weapon owned (-1 when there are no copies).</summary>
        public int BestWeaponGrade(string weaponId) => Best(WeaponCopies, weaponId);

        // ------------------------------------------------------------------ equipment

        public bool IsEquipmentEquipped(string equipmentId) =>
            EquippedEquipmentIds != null && EquippedEquipmentIds.Contains(equipmentId);

        /// <summary>Copies of this equipment at every grade.</summary>
        public int GetEquipmentCount(string equipmentId)
        {
            int total = 0;
            foreach (var e in Equipment)
                if (e != null && e.Id == equipmentId) total += e.Count;
            return total;
        }

        public int GetEquipmentCount(string equipmentId, int grade) => Count(Equipment, equipmentId, grade);

        /// <summary>Best grade of this equipment owned (-1 when none).</summary>
        public int BestEquipmentGrade(string equipmentId) => Best(Equipment, equipmentId);

        public void AddEquipment(string equipmentId, int count = 1, int grade = 0) => AddCopies(Equipment, equipmentId, count, grade);

        /// <summary>Removes copies at one grade; unequips the item once no copy of any grade is left.</summary>
        public bool RemoveEquipment(string equipmentId, int count, int grade)
        {
            if (!RemoveCopies(Equipment, equipmentId, count, grade)) return false;
            if (GetEquipmentCount(equipmentId) <= 0) EquippedEquipmentIds.Remove(equipmentId);
            return true;
        }

        // ------------------------------------------------------------------ stacks

        private static int Count(List<OwnedEquipment> stacks, string id, int grade)
        {
            foreach (var e in stacks)
                if (e != null && e.Id == id && e.Grade == grade) return e.Count;
            return 0;
        }

        private static int Best(List<OwnedEquipment> stacks, string id)
        {
            int best = -1;
            foreach (var e in stacks)
                if (e != null && e.Id == id && e.Count > 0 && e.Grade > best) best = e.Grade;
            return best;
        }

        private static void AddCopies(List<OwnedEquipment> stacks, string id, int count, int grade)
        {
            if (string.IsNullOrEmpty(id) || count <= 0) return;
            foreach (var e in stacks)
            {
                if (e == null || e.Id != id || e.Grade != grade) continue;
                e.Count += count;
                return;
            }
            stacks.Add(new OwnedEquipment { Id = id, Count = count, Grade = grade });
        }

        private static bool RemoveCopies(List<OwnedEquipment> stacks, string id, int count, int grade)
        {
            for (int i = 0; i < stacks.Count; i++)
            {
                var e = stacks[i];
                if (e == null || e.Id != id || e.Grade != grade) continue;
                if (e.Count < count) return false;
                e.Count -= count;
                if (e.Count <= 0) stacks.RemoveAt(i);
                return true;
            }
            return false;
        }
    }
}
