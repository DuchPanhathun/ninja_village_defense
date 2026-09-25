using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Gameplay.Loot;
using NinjaVillage.Gameplay.Weapons;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Inventory;
using NinjaVillage.Systems.Mounts;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Crates
{
    public enum CrateDropKind
    {
        Gear,
        Weapon,
        Mount,
    }

    /// <summary>One item out of a crate.</summary>
    public readonly struct CrateDrop
    {
        public readonly CrateDropKind Kind;
        public readonly string Id;
        public readonly string Name;
        public readonly ItemGrade Grade;
        public readonly bool Special;
        /// <summary>The first copy of this item you own.</summary>
        public readonly bool New;

        public bool Weapon => Kind == CrateDropKind.Weapon;
        public bool Mount => Kind == CrateDropKind.Mount;

        public CrateDrop(CrateDropKind kind, string id, string name, ItemGrade grade, bool special, bool isNew)
        {
            Kind = kind;
            Id = id;
            Name = name;
            Grade = grade;
            Special = special;
            New = isNew;
        }
    }

    public enum CrateResult
    {
        Opened,
        UnknownCrate,
        NotEnoughCurrency,
        FreeUsed,
        Empty,
    }

    /// <summary>
    /// Supply crates (the Crates screen): pay (or open the free daily Wooden Crate), roll each open with
    /// <see cref="CrateRules"/>, and hand over the items — weapon copies and gear at the rolled grade, S-class pieces
    /// from Surprise Boxes (which also hold the S-class mounts). State in <c>SaveService.Data.Crates</c>.
    /// </summary>
    public static class CrateService
    {
        private static CrateSaveData Data => SaveService.Data.Crates;

        public static bool CanOpenFree(CrateKind crate) => crate != null && crate.DailyFree && Data.FreeDay != GameClock.Today;
        public static bool FreeAvailable => CanOpenFree(CrateRules.Get("wood"));

        /// <summary>Surprise Boxes left until an S item is certain.</summary>
        public static int PityLeft(CrateKind crate) => crate == null || crate.Pity <= 0 ? 0 : System.Math.Max(1, crate.Pity - Data.SurprisePity);

        public static Price PriceFor(CrateKind crate, int count) =>
            crate == null ? default : new Price(crate.Price.Currency, crate.Price.Amount * System.Math.Max(1, count));

        // ------------------------------------------------------------------ pools

        public static List<EquipmentDefinition> RegularGear() => Gear(false);
        public static List<EquipmentDefinition> SpecialGear() => Gear(true);
        public static List<WeaponDefinition> RegularWeapons() => Weapons(false);
        public static List<WeaponDefinition> SpecialWeapons() => Weapons(true);

        /// <summary>S-class mounts (Surprise Boxes only; a duplicate levels the mount up).</summary>
        public static List<MountDefinition> SpecialMounts()
        {
            var list = MountService.GetSorted();
            list.RemoveAll(m => !m.IsSpecial);
            return list;
        }

        private static List<EquipmentDefinition> Gear(bool special)
        {
            var list = new List<EquipmentDefinition>();
            var catalog = InventoryService.EquipmentDefs;
            if (catalog != null)
                foreach (var def in catalog.All)
                    if (def != null && !string.IsNullOrEmpty(def.Id) && def.IsSpecial == special) list.Add(def);
            return list;
        }

        private static List<WeaponDefinition> Weapons(bool special)
        {
            var list = new List<WeaponDefinition>();
            var catalog = InventoryService.Weapons;
            if (catalog != null)
                foreach (var def in catalog.All)
                    if (def != null && !string.IsNullOrEmpty(def.Id) && def.IsSpecial == special) list.Add(def);
            return list;
        }

        // ------------------------------------------------------------------ opening

        public static CrateResult Check(CrateKind crate, int count, bool free)
        {
            if (crate == null) return CrateResult.UnknownCrate;
            if (RegularGear().Count == 0) return CrateResult.Empty;
            if (free) return CanOpenFree(crate) && count == 1 ? CrateResult.Opened : CrateResult.FreeUsed;
            return CurrencyService.CanAfford(PriceFor(crate, count)) ? CrateResult.Opened : CrateResult.NotEnoughCurrency;
        }

        /// <summary>Opens <paramref name="count"/> crates (paid, or the free daily one) and grants what's inside.</summary>
        public static CrateResult Open(string crateId, int count, bool free, out List<CrateDrop> drops, System.Random rng = null)
        {
            drops = new List<CrateDrop>();
            var crate = CrateRules.Get(crateId);
            count = System.Math.Max(1, count);
            var check = Check(crate, count, free);
            if (check != CrateResult.Opened) return check;
            if (free) Data.FreeDay = GameClock.Today;
            else if (!CurrencyService.TrySpend(PriceFor(crate, count), $"crate_{crate.Id}")) return CrateResult.NotEnoughCurrency;

            rng ??= new System.Random();
            var gear = RegularGear();
            var weapons = RegularWeapons();
            var specials = new List<(CrateDropKind kind, string id)>();
            foreach (var w in SpecialWeapons()) specials.Add((CrateDropKind.Weapon, w.Id));
            foreach (var g in SpecialGear()) specials.Add((CrateDropKind.Gear, g.Id));
            if (crate.SMounts)
                foreach (var m in SpecialMounts()) specials.Add((CrateDropKind.Mount, m.Id));

            for (int i = 0; i < count; i++)
            {
                var roll = CrateRules.Roll(crate, rng, crate.Pity > 0 ? Data.SurprisePity : 0);
                if (roll.Special && specials.Count > 0)
                {
                    var (kind, id) = specials[rng.Next(specials.Count)];
                    drops.Add(Grant(kind, id, roll.Grade, true));
                    Data.SurprisePity = 0;
                    Data.SpecialsFound++;
                    continue;
                }
                if (crate.Pity > 0) Data.SurprisePity++;
                bool asWeapon = roll.Weapon && weapons.Count > 0;
                string pick = asWeapon ? weapons[rng.Next(weapons.Count)].Id : gear[rng.Next(gear.Count)].Id;
                drops.Add(Grant(asWeapon ? CrateDropKind.Weapon : CrateDropKind.Gear, pick, roll.Grade, false));
            }
            Data.Opened += count;
            SaveService.MarkDirty();
            Progress.Report(ProgressStatIds.CrateOpened, count, crate.Id);
            return CrateResult.Opened;
        }

        private static CrateDrop Grant(CrateDropKind kind, string id, ItemGrade grade, bool special)
        {
            if (kind == CrateDropKind.Mount)
            {
                var mount = MountService.Get(id);
                bool isNew = MountService.Grant(mount);
                return new CrateDrop(kind, id, mount != null ? mount.NameOrId : id, ItemGrade.Legendary, special, isNew);
            }
            if (kind == CrateDropKind.Weapon)
            {
                bool isNew = InventoryService.GrantWeapon(id, grade);
                var def = InventoryService.GetWeapon(id);
                return new CrateDrop(kind, id, def != null && !string.IsNullOrEmpty(def.DisplayName) ? def.DisplayName : id, grade, special, isNew);
            }
            bool fresh = InventoryService.Data.GetEquipmentCount(id) == 0;
            InventoryService.AddEquipment(id, 1, grade);
            var item = InventoryService.GetEquipment(id);
            return new CrateDrop(kind, id, item != null && !string.IsNullOrEmpty(item.DisplayName) ? item.DisplayName : id, grade, special, fresh);
        }

        public static string Describe(CrateResult result, CrateKind crate) => result switch
        {
            CrateResult.NotEnoughCurrency => crate != null && crate.Price.Currency == CurrencyType.Gems ? "Not enough gems." : "Not enough coins.",
            CrateResult.FreeUsed => "Today's free crate is already opened. A new one tomorrow!",
            CrateResult.Empty => "No equipment found. Run Ninja Village → Generate Default Content.",
            CrateResult.UnknownCrate => "Unknown crate.",
            _ => string.Empty,
        };
    }
}
