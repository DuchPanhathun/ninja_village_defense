using System.Collections.Generic;
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
        AlreadyOwned,
        MaxLevel,
        MaxTier,
        WeaponLevel,
        NotEnoughMaterials,
        NotEnoughCurrency
    }

    /// <summary>
    /// The Forge (EPIC 11): crafting weapons the player doesn't own, upgrading weapon levels (capped by
    /// the Forge's level — its data-defined effect), and reforging a weapon into the next tier
    /// (Iron → Steel → Golden → Legendary, goal.text) using coins plus spare equipment as materials — or the
    /// mine's metal bars in place of missing gear (EPIC 24 Phase 5).
    /// Costs and requirements come from <see cref="EconomyConfig"/>.
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

        // ------------------------------------------------------------------ craft

        public static Price CraftPrice(WeaponDefinition def) =>
            def != null ? EconomyConfig.Current.CraftPrice(def.Rarity) : default;

        public static int CraftForgeLevel(WeaponDefinition def) =>
            def != null ? EconomyConfig.Current.CraftForgeLevel(def.Rarity) : 0;

        public static ForgeBlocker CheckCraft(WeaponDefinition def)
        {
            if (def == null) return ForgeBlocker.NoDefinition;
            if (InventoryService.Data.OwnsWeapon(def.Id)) return ForgeBlocker.AlreadyOwned;
            if (ForgeLevel <= 0) return ForgeBlocker.ForgeNotBuilt;
            if (ForgeLevel < CraftForgeLevel(def)) return ForgeBlocker.ForgeLevel;
            if (!CurrencyService.CanAfford(CraftPrice(def))) return ForgeBlocker.NotEnoughCurrency;
            return ForgeBlocker.None;
        }

        public static bool TryCraft(WeaponDefinition def, out ForgeBlocker blocker)
        {
            blocker = CheckCraft(def);
            if (blocker != ForgeBlocker.None) return false;

            if (!CurrencyService.TrySpend(CraftPrice(def), def.Id))
            {
                blocker = ForgeBlocker.NotEnoughCurrency;
                return false;
            }

            InventoryService.AddWeapon(def.Id);
            Progress.Report(ProgressStatIds.WeaponCrafted, 1, def.Id);
            return true;
        }

        // ------------------------------------------------------------------ reforge (tiers)

        public static int GetTier(string weaponId) => InventoryService.Data.GetWeaponTier(weaponId);

        public static string TierName(int tier)
        {
            var config = EconomyConfig.Current.GetTier(tier);
            return config != null ? config.Name : string.Empty;
        }

        /// <summary>Attack bonus of the weapon's current tier (applied at run start by <see cref="InventoryRunModifier"/>).</summary>
        public static float TierAttackBonus(string weaponId)
        {
            var config = EconomyConfig.Current.GetTier(GetTier(weaponId));
            return config != null ? config.AttackBonus : 0f;
        }

        /// <summary>The next tier's requirements, or null at the top tier.</summary>
        public static WeaponTierConfig NextTier(string weaponId) => EconomyConfig.Current.GetTier(GetTier(weaponId) + 1);

        private static readonly List<string> MaterialBuffer = new();

        public static ForgeBlocker CheckReforge(WeaponDefinition def)
        {
            if (def == null) return ForgeBlocker.NoDefinition;
            var inv = InventoryService.Data;
            if (!inv.OwnsWeapon(def.Id)) return ForgeBlocker.NotOwned;
            var next = NextTier(def.Id);
            if (next == null) return ForgeBlocker.MaxTier;
            if (ForgeLevel <= 0) return ForgeBlocker.ForgeNotBuilt;
            if (ForgeLevel < next.RequiredForgeLevel) return ForgeBlocker.ForgeLevel;
            if (GetLevel(def.Id) < next.RequiredWeaponLevel) return ForgeBlocker.WeaponLevel;
            var (bar, bars) = MaterialsFor(def, next);
            if (bars > 0 && Farm.GoodsService.Count(bar) < bars) return ForgeBlocker.NotEnoughMaterials;
            if (!CurrencyService.CanAfford(Price.Coins(next.CoinCost))) return ForgeBlocker.NotEnoughCurrency;
            return ForgeBlocker.None;
        }

        /// <summary>
        /// Materials for reforging <paramref name="def"/> into <paramref name="next"/>: spare gear first (picked into
        /// the material buffer), and for every piece still missing, <see cref="PondMineRules.BarsPerMaterial"/> of
        /// the tier's metal bar from the mine. Returns that bar and how many are needed (0 when gear covers it all).
        /// </summary>
        private static (string bar, int bars) MaterialsFor(WeaponDefinition def, WeaponTierConfig next)
        {
            InventoryRules.PickMaterials(InventoryService.Data, InventoryService.RarityIndexOf, (int)next.MaterialMinRarity, next.MaterialCount, MaterialBuffer);
            int missing = Mathf.Max(0, next.MaterialCount - MaterialBuffer.Count);
            string bar = PondMineRules.ReforgeBar(GetTier(def.Id) + 1);
            return missing == 0 || bar == null ? (bar, 0) : (bar, missing * PondMineRules.BarsPerMaterial);
        }

        /// <summary>"2 spare Common+ gear (or 5 Iron Bars each) · you'd use 1 gear + 5 Iron Bars" for the Forge screen.</summary>
        public static string DescribeMaterials(WeaponDefinition def)
        {
            var next = def != null ? NextTier(def.Id) : null;
            if (next == null || next.MaterialCount <= 0) return string.Empty;
            string barId = PondMineRules.ReforgeBar(GetTier(def.Id) + 1);
            var barGoods = barId != null ? Farm.GoodsService.Get(barId) : null;
            string barName = barGoods != null ? barGoods.NameOrId + "s" : "bars";
            string text = $"{next.MaterialCount} spare {next.MaterialMinRarity}+ gear (or {PondMineRules.BarsPerMaterial} {barName} each)";
            if (!InventoryService.Data.OwnsWeapon(def.Id)) return text;
            var (bar, bars) = MaterialsFor(def, next);
            int gear = MaterialBuffer.Count;
            return bars <= 0 ? $"{text} · you have the gear"
                : $"{text} · uses {gear} gear + {bars} {barName} (have {Farm.GoodsService.Count(bar)})";
        }

        public static bool TryReforge(WeaponDefinition def, out ForgeBlocker blocker)
        {
            blocker = CheckReforge(def);
            if (blocker != ForgeBlocker.None) return false;

            var next = NextTier(def.Id);
            if (!CurrencyService.TrySpend(Price.Coins(next.CoinCost), def.Id))
            {
                blocker = ForgeBlocker.NotEnoughCurrency;
                return false;
            }

            var inv = InventoryService.Data;
            var (bar, bars) = MaterialsFor(def, next); // fills MaterialBuffer with the spare gear used
            InventoryRules.ConsumeMaterials(inv, MaterialBuffer);
            if (bars > 0) Farm.GoodsService.TrySpend(bar, bars);
            inv.SetWeaponTier(def.Id, GetTier(def.Id) + 1);
            SaveService.SaveNow();

            Progress.Report(ProgressStatIds.WeaponCrafted, 1, def.Id);
            InventoryService.Raise(InventoryChangeKind.WeaponTierChanged, def.Id);
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
                    if (!InventoryService.Data.OwnsWeapon(def.Id)) return $"Requires Forge Lv {CraftForgeLevel(def)}";
                    var next = NextTier(def.Id);
                    return next != null && ForgeLevel < next.RequiredForgeLevel && GetLevel(def.Id) < MaxWeaponLevel
                        ? $"Requires Forge Lv {next.RequiredForgeLevel}"
                        : "Upgrade the Forge to raise the level cap";
                case ForgeBlocker.NotOwned: return "Craft this weapon first";
                case ForgeBlocker.AlreadyOwned: return "Already owned";
                case ForgeBlocker.MaxLevel: return "Max level";
                case ForgeBlocker.MaxTier: return "Max tier";
                case ForgeBlocker.WeaponLevel:
                    return def != null && NextTier(def.Id) != null ? $"Weapon must be Lv {NextTier(def.Id).RequiredWeaponLevel}" : "Weapon level too low";
                case ForgeBlocker.NotEnoughMaterials:
                    if (def == null || NextTier(def.Id) == null) return "Not enough materials";
                    var tier = NextTier(def.Id);
                    return $"Needs {tier.MaterialCount} spare {tier.MaterialMinRarity}+ equipment, or {PondMineRules.BarsPerMaterial} mine bars per missing piece";
                case ForgeBlocker.NotEnoughCurrency: return "Not enough coins";
                default: return "Unavailable";
            }
        }
    }
}
