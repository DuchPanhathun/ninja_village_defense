using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Inventory
{
    public enum MergeResult
    {
        Merged,
        UnknownItem,
        MaxGrade,
        NotEnoughCopies,
    }

    /// <summary>
    /// Merging (Survivor.io-style): three copies of the same weapon or gear piece at the same grade become one of
    /// the next grade — Common → Rare → Elite → Epic → Legendary. With only two copies, metal bars from the mine
    /// can stand in for the third (<see cref="GradeRules.BarsFor"/>). An equipped item automatically uses the best
    /// grade owned, so merging it up makes it stronger straight away. Rules in <see cref="GradeRules"/>.
    /// </summary>
    public static class MergeService
    {
        private static InventorySaveData Data => InventoryService.Data;

        private static int Bars(ItemGrade grade)
        {
            var (bar, _) = GradeRules.BarsFor(GradeRules.Next(grade));
            return bar != null ? GoodsService.Count(bar) : 0;
        }

        // ------------------------------------------------------------------ gear

        public static MergeResult CheckGear(string equipmentId, ItemGrade grade)
        {
            if (InventoryService.GetEquipment(equipmentId) == null) return MergeResult.UnknownItem;
            if (GradeRules.IsMax(grade)) return MergeResult.MaxGrade;
            return GradeRules.CanMerge(grade, Data.GetEquipmentCount(equipmentId, (int)grade), Bars(grade))
                ? MergeResult.Merged : MergeResult.NotEnoughCopies;
        }

        public static MergeResult MergeGear(string equipmentId, ItemGrade grade)
        {
            var check = CheckGear(equipmentId, grade);
            if (check != MergeResult.Merged) return check;
            var inv = Data;
            int copies = System.Math.Min(GradeRules.MergeCount, inv.GetEquipmentCount(equipmentId, (int)grade));
            bool wasEquipped = inv.IsEquipmentEquipped(equipmentId);
            inv.AddEquipment(equipmentId, 1, (int)GradeRules.Next(grade)); // add first so the item never looks unowned
            inv.RemoveEquipment(equipmentId, copies, (int)grade);
            if (wasEquipped && !inv.IsEquipmentEquipped(equipmentId)) inv.EquippedEquipmentIds.Add(equipmentId);
            PayBars(grade, copies);
            Done(equipmentId, InventoryChangeKind.EquipmentMerged);
            return MergeResult.Merged;
        }

        // ------------------------------------------------------------------ weapons

        public static MergeResult CheckWeapon(string weaponId, ItemGrade grade)
        {
            if (InventoryService.GetWeapon(weaponId) == null || !Data.OwnsWeapon(weaponId)) return MergeResult.UnknownItem;
            if (GradeRules.IsMax(grade)) return MergeResult.MaxGrade;
            return GradeRules.CanMerge(grade, Data.GetWeaponCopies(weaponId, (int)grade), Bars(grade))
                ? MergeResult.Merged : MergeResult.NotEnoughCopies;
        }

        public static MergeResult MergeWeapon(string weaponId, ItemGrade grade)
        {
            var check = CheckWeapon(weaponId, grade);
            if (check != MergeResult.Merged) return check;
            var inv = Data;
            int copies = System.Math.Min(GradeRules.MergeCount, inv.GetWeaponCopies(weaponId, (int)grade));
            inv.AddWeaponCopies(weaponId, 1, (int)GradeRules.Next(grade));
            inv.RemoveWeaponCopies(weaponId, copies, (int)grade);
            PayBars(grade, copies);
            Done(weaponId, InventoryChangeKind.WeaponMerged);
            return MergeResult.Merged;
        }

        // ------------------------------------------------------------------ everything

        /// <summary>
        /// Merges every full set of three, lowest grades first so new copies can merge again (bars are never
        /// used here — that's a choice per item). Returns how many merges happened.
        /// </summary>
        public static int MergeAll()
        {
            var inv = Data;
            int merges = 0;
            for (int g = 0; g < (int)GradeRules.Max; g++)
            {
                var grade = (ItemGrade)g;
                foreach (var stack in inv.Equipment.ToArray())
                    while (stack != null && stack.Grade == g && inv.GetEquipmentCount(stack.Id, g) >= GradeRules.MergeCount
                           && MergeGear(stack.Id, grade) == MergeResult.Merged) merges++;
                foreach (var stack in inv.WeaponCopies.ToArray())
                    while (stack != null && stack.Grade == g && inv.GetWeaponCopies(stack.Id, g) >= GradeRules.MergeCount
                           && MergeWeapon(stack.Id, grade) == MergeResult.Merged) merges++;
            }
            return merges;
        }

        /// <summary>The lowest grade of this weapon that can merge now (with bars if needed), or null.</summary>
        public static ItemGrade? NextWeaponMerge(string weaponId)
        {
            for (int g = 0; g < (int)GradeRules.Max; g++)
                if (CheckWeapon(weaponId, (ItemGrade)g) == MergeResult.Merged) return (ItemGrade)g;
            return null;
        }

        /// <summary>"Common ×2 · Rare ×1" — a weapon's copies by grade.</summary>
        public static string DescribeWeaponCopies(string weaponId)
        {
            var parts = new System.Collections.Generic.List<string>();
            for (int g = 0; g <= (int)GradeRules.Max; g++)
            {
                int n = Data.GetWeaponCopies(weaponId, g);
                if (n > 0) parts.Add(GradeColors.Colorize($"{(ItemGrade)g} ×{n}", (ItemGrade)g));
            }
            return string.Join("  ·  ", parts);
        }

        /// <summary>How many full sets of three are waiting to be merged (for the "Merge all" button / badges).</summary>
        public static int MergeableCount()
        {
            var inv = Data;
            int sets = 0;
            foreach (var stack in inv.Equipment)
                if (stack != null && stack.Grade < (int)GradeRules.Max) sets += stack.Count / GradeRules.MergeCount;
            foreach (var stack in inv.WeaponCopies)
                if (stack != null && stack.Grade < (int)GradeRules.Max) sets += stack.Count / GradeRules.MergeCount;
            return sets;
        }

        public static string Describe(MergeResult result, ItemGrade grade)
        {
            var (bar, amount) = GradeRules.BarsFor(GradeRules.Next(grade));
            var goods = bar != null ? GoodsService.Get(bar) : null;
            return result switch
            {
                MergeResult.MaxGrade => "Already Legendary — the best grade there is.",
                MergeResult.NotEnoughCopies => goods != null
                    ? $"Needs {GradeRules.MergeCount} {grade} copies (or 2 + {amount} {goods.NameOrId}s)."
                    : $"Needs {GradeRules.MergeCount} {grade} copies.",
                MergeResult.UnknownItem => "Unknown item.",
                _ => string.Empty,
            };
        }

        /// <summary>Bars pay for the missing copy when only two were merged.</summary>
        private static void PayBars(ItemGrade grade, int copiesUsed)
        {
            if (copiesUsed >= GradeRules.MergeCount) return;
            var (bar, amount) = GradeRules.BarsFor(GradeRules.Next(grade));
            if (bar != null) GoodsService.TrySpend(bar, amount);
        }

        private static void Done(string itemId, InventoryChangeKind kind)
        {
            SaveService.MarkDirty();
            Progress.Report(ProgressStatIds.ItemMerged, 1, itemId);
            InventoryService.Raise(kind, itemId);
        }
    }
}
