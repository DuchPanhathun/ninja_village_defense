using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Weapons;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using UnityEngine;

namespace NinjaVillage.Systems.Inventory
{
    public enum ForgeBlocker
    {
        None,
        NoDefinition,
        ForgeNotBuilt,
        ForgeLevel,
        NotOwned,
        MaxLevel,
        NotEnoughCurrency,
        /// <summary>S-class weapons come only from Surprise Boxes.</summary>
        NotCraftable
    }

    /// <summary>
    /// The Forge (EPIC 11): crafting weapons (the first time it unlocks the weapon; after that each craft forges
    /// another copy to merge into a better grade — see <see cref="MergeService"/>) and upgrading weapon levels
    /// (capped by the Forge's level — its data-defined effect). Costs come from <see cref="EconomyConfig"/>.
    /// </summary>
    public static class ForgeService
    {
        public static int ForgeLevel => VillageService.GetLevel(BuildingIds.Forge);

        /// <summary>Highest weapon level the Forge allows right now (1 until the Forge is built).</summary>
        public static int MaxWeaponLevel
        {
            get
            {
                if (ForgeLevel <= 0) return 1;
                int cap = VillageRules.FloorEffect(VillageService.GetEffect(BuildingIds.Forge, RuntimeWeapon.MaxLevel));
                return Mathf.Clamp(cap, 1, RuntimeWeapon.MaxLevel);
            }
        }

        // ------------------------------------------------------------------ upgrade

        public static int GetLevel(string weaponId) => Mathf.Max(1, InventoryService.Data.GetWeaponLevel(weaponId));

        public static Price UpgradePrice(WeaponDefinition def) =>
            def != null ? EconomyConfig.Current.WeaponUpgradePrice(def.Rarity, GetLevel(def.Id)) : default;

        public static ForgeBlocker CheckUpgrade(WeaponDefinition def)
        {
            if (def == null) return ForgeBlocker.NoDefinition;
            var inv = InventoryService.Data;
            if (!inv.OwnsWeapon(def.Id)) return ForgeBlocker.NotOwned;
            int level = GetLevel(def.Id);
            if (level >= RuntimeWeapon.MaxLevel) return ForgeBlocker.MaxLevel;
            if (ForgeLevel <= 0) return ForgeBlocker.ForgeNotBuilt;
            if (level >= MaxWeaponLevel) return ForgeBlocker.ForgeLevel;
            if (!CurrencyService.CanAfford(UpgradePrice(def))) return ForgeBlocker.NotEnoughCurrency;
            return ForgeBlocker.None;
        }

        public static bool TryUpgrade(WeaponDefinition def, out ForgeBlocker blocker)
        {
            blocker = CheckUpgrade(def);
            if (blocker != ForgeBlocker.None) return false;

            if (!CurrencyService.TrySpend(UpgradePrice(def), def.Id))
            {
                blocker = ForgeBlocker.NotEnoughCurrency;
                return false;
            }

            int newLevel = GetLevel(def.Id) + 1;
            InventoryService.Data.Weapons.SetLevel(def.Id, newLevel);
            SaveService.SaveNow();
            Progress.Report(ProgressStatIds.WeaponUpgraded, 1, def.Id);
            InventoryService.Raise(InventoryChangeKind.WeaponUpgraded, def.Id);
            return true;
        }

        // ------------------------------------------------------------------ craft (unlock, then copies)

        public static Price CraftPrice(WeaponDefinition def) =>
            def != null ? EconomyConfig.Current.CraftPrice(def.Rarity) : default;

        public static int CraftForgeLevel(WeaponDefinition def) =>
            def != null ? EconomyConfig.Current.CraftForgeLevel(def.Rarity) : 0;

        public static bool IsOwned(WeaponDefinition def) => def != null && InventoryService.Data.OwnsWeapon(def.Id);

        public static ForgeBlocker CheckCraft(WeaponDefinition def)
        {
            if (def == null) return ForgeBlocker.NoDefinition;
            if (def.IsSpecial) return ForgeBlocker.NotCraftable;
            if (ForgeLevel <= 0) return ForgeBlocker.ForgeNotBuilt;
            if (ForgeLevel < CraftForgeLevel(def)) return ForgeBlocker.ForgeLevel;
            if (!CurrencyService.CanAfford(CraftPrice(def))) return ForgeBlocker.NotEnoughCurrency;
            return ForgeBlocker.None;
        }

        /// <summary>Crafts the weapon: unlocks it the first time, then forges another copy (its native grade) for merging.</summary>
        public static bool TryCraft(WeaponDefinition def, out ForgeBlocker blocker)
        {
            blocker = CheckCraft(def);
            if (blocker != ForgeBlocker.None) return false;

            if (!CurrencyService.TrySpend(CraftPrice(def), def.Id))
            {
                blocker = ForgeBlocker.NotEnoughCurrency;
                return false;
            }

            if (IsOwned(def)) InventoryService.AddWeaponCopy(def.Id);
            else InventoryService.AddWeapon(def.Id);
            SaveService.SaveNow();
            Progress.Report(ProgressStatIds.WeaponCrafted, 1, def.Id);
            return true;
        }

        public static string DescribeBlocker(ForgeBlocker blocker, WeaponDefinition def)
        {
            switch (blocker)
            {
                case ForgeBlocker.None: return string.Empty;
                case ForgeBlocker.NoDefinition: return "Unknown weapon";
                case ForgeBlocker.ForgeNotBuilt: return "Build the Forge first";
                case ForgeBlocker.ForgeLevel:
                    if (def == null) return "Upgrade the Forge";
                    return !InventoryService.Data.OwnsWeapon(def.Id) || GetLevel(def.Id) < MaxWeaponLevel
                        ? $"Requires Forge Lv {CraftForgeLevel(def)}"
                        : "Upgrade the Forge to raise the level cap";
                case ForgeBlocker.NotOwned: return "Craft this weapon first";
                case ForgeBlocker.MaxLevel: return "Max level";
                case ForgeBlocker.NotEnoughCurrency: return "Not enough coins";
                case ForgeBlocker.NotCraftable: return "S-class: only found in Surprise Boxes";
                default: return "Unavailable";
            }
        }
    }
}
