using System.Collections.Generic;
using NinjaVillage.Core.Data;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Heroes;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Heroes
{
    /// <summary>
    /// Hero roster operations for the UI and other systems (EPIC 13): unlock, select and upgrade
    /// heroes, paying through <see cref="EconomyManager"/> with prices/gates from
    /// <see cref="HeroRules"/>. State lives in <c>SaveService.Data.Heroes</c>; definitions come
    /// from the <see cref="HeroCatalog"/>. The free starter hero is granted automatically the
    /// first time anything asks (and again after a save reset / cloud replace).
    /// </summary>
    public static class HeroService
    {
        private static readonly List<HeroDefinition> EnsureBuffer = new();
        private static readonly List<string> StarterIdBuffer = new();

        public static HeroCatalog Catalog => CatalogLoader.Load<HeroCatalog>();

        private static HeroSaveData Section => SaveService.Data.Heroes;

        public static int DojoLevel => SaveService.Data.Village.GetBuildingLevel(BuildingIds.Dojo);

        /// <summary>Catalog heroes ordered by sort order (then catalog order). A new list per call (UI use).</summary>
        public static List<HeroDefinition> GetSortedHeroes()
        {
            var list = new List<HeroDefinition>();
            FillSorted(list);
            return list;
        }

        private static void FillSorted(List<HeroDefinition> into)
        {
            into.Clear();
            var catalog = Catalog;
            if (catalog == null) return;

            foreach (var hero in catalog.All)
                if (hero != null) into.Add(hero);

            // Insertion sort keeps catalog order for equal sort orders (List.Sort isn't stable).
            for (int i = 1; i < into.Count; i++)
            {
                var current = into[i];
                int j = i - 1;
                while (j >= 0 && into[j].SortOrder > current.SortOrder)
                {
                    into[j + 1] = into[j];
                    j--;
                }
                into[j + 1] = current;
            }
        }

        public static HeroDefinition Get(string heroId)
        {
            var catalog = Catalog;
            return catalog != null ? catalog.Get(heroId) : null;
        }

        /// <summary>
        /// Grants the free starter hero(es) and repairs the selection. Cheap and idempotent — every
        /// public entry point calls it, so the first hero exists before any UI or run needs it.
        /// </summary>
        public static void EnsureDefaults() => EnsureDefaults(SaveService.Data);

        private static void EnsureDefaults(SaveData data)
        {
            if (data == null || data.Heroes == null) return;
            var catalog = Catalog;
            if (catalog == null) return;

            FillSorted(EnsureBuffer);
            StarterIdBuffer.Clear();
            foreach (var hero in EnsureBuffer)
                if (hero.UnlockedByDefault) StarterIdBuffer.Add(hero.Id);

            // No hero flagged as starter → the first (lowest sort order) hero is the free default.
            if (StarterIdBuffer.Count == 0 && EnsureBuffer.Count > 0)
                StarterIdBuffer.Add(EnsureBuffer[0].Id);
            EnsureBuffer.Clear();

            // Drop selections of heroes that no longer exist in the catalog.
            if (!string.IsNullOrEmpty(data.Heroes.SelectedHeroId) && catalog.Get(data.Heroes.SelectedHeroId) == null)
                data.Heroes.SelectedHeroId = null;

            if (HeroRules.EnsureStarterHeroes(data.Heroes, StarterIdBuffer))
                SaveService.MarkDirty();
        }

        public static bool IsUnlocked(HeroDefinition hero)
        {
            if (hero == null) return false;
            EnsureDefaults();
            return Section.IsUnlocked(hero.Id);
        }

        /// <summary>0 when locked.</summary>
        public static int GetLevel(HeroDefinition hero)
        {
            if (hero == null) return 0;
            EnsureDefaults();
            return Section.GetLevel(hero.Id);
        }

        public static int GetLevelCap(HeroDefinition hero) =>
            hero == null ? 0 : HeroRules.GetLevelCap(DojoLevel, hero.MaxLevel);

        /// <summary>Coins for the next level (the cost at level 1 if locked).</summary>
        public static int GetUpgradeCost(HeroDefinition hero) =>
            hero == null ? 0 : HeroRules.GetUpgradeCost(Mathf.Max(1, GetLevel(hero)), hero.UpgradeCostBase, hero.UpgradeCostExponent);

        public static HeroDefinition GetSelected()
        {
            EnsureDefaults();
            return Get(Section.SelectedHeroId);
        }

        public static bool IsSelected(HeroDefinition hero) => hero != null && GetSelected() == hero;

        public static HeroActionResult CheckUnlock(HeroDefinition hero)
        {
            if (hero == null) return HeroActionResult.InvalidHero;
            int balance = SaveService.Data.Wallet.Get(hero.UnlockCurrency);
            return HeroRules.CheckUnlock(IsUnlocked(hero), DojoLevel, hero.RequiredDojoLevel, balance, hero.UnlockCost);
        }

        public static HeroActionResult TryUnlock(HeroDefinition hero)
        {
            var result = CheckUnlock(hero);
            if (result != HeroActionResult.Success) return result;
            if (!Spend(hero.UnlockCurrency, hero.UnlockCost)) return HeroActionResult.NotEnoughCurrency;

            GrantInternal(hero.Id);
            return HeroActionResult.Success;
        }

        /// <summary>
        /// Unlocks a hero without charging — store purchases / premium bundles (EPIC 21), rewards.
        /// Returns false when the id is unknown or already owned.
        /// </summary>
        public static bool Grant(string heroId)
        {
            var hero = Get(heroId);
            if (hero == null) return false;
            EnsureDefaults();
            if (Section.IsUnlocked(hero.Id)) return false;
            GrantInternal(hero.Id);
            return true;
        }

        private static void GrantInternal(string heroId)
        {
            Section.Owned.SetLevel(heroId, 1);
            SaveService.SaveNow();
            Progress.Report(ProgressStatIds.HeroUnlocked, 1, heroId);
            EventBus<HeroUnlockedEvent>.Raise(new HeroUnlockedEvent(heroId));
        }

        public static HeroActionResult TrySelect(HeroDefinition hero)
        {
            if (hero == null) return HeroActionResult.InvalidHero;
            if (!IsUnlocked(hero)) return HeroActionResult.NotUnlocked;
            if (Section.SelectedHeroId == hero.Id) return HeroActionResult.Success;

            Section.SelectedHeroId = hero.Id;
            SaveService.MarkDirty();
            EventBus<HeroSelectedEvent>.Raise(new HeroSelectedEvent(hero.Id));
            return HeroActionResult.Success;
        }

        public static HeroActionResult CheckUpgrade(HeroDefinition hero)
        {
            if (hero == null) return HeroActionResult.InvalidHero;
            bool owned = IsUnlocked(hero);
            int level = Section.GetLevel(hero.Id);
            int cost = GetUpgradeCost(hero);
            int balance = SaveService.Data.Wallet.Get(CurrencyType.Coins);
            return HeroRules.CheckUpgrade(owned, level, GetLevelCap(hero), hero.MaxLevel, balance, cost);
        }

        public static HeroActionResult TryUpgrade(HeroDefinition hero)
        {
            var result = CheckUpgrade(hero);
            if (result != HeroActionResult.Success) return result;

            int cost = GetUpgradeCost(hero);
            if (!Spend(CurrencyType.Coins, cost)) return HeroActionResult.NotEnoughCurrency;

            int newLevel = Section.GetLevel(hero.Id) + 1;
            Section.Owned.SetLevel(hero.Id, newLevel);
            SaveService.SaveNow();

            Progress.Report(ProgressStatIds.HeroUpgraded, 1, hero.Id);
            EventBus<HeroLevelChangedEvent>.Raise(new HeroLevelChangedEvent(hero.Id, newLevel));
            return HeroActionResult.Success;
        }

        /// <summary>Player-facing message for a failed action.</summary>
        public static string Describe(HeroActionResult result, HeroDefinition hero)
        {
            switch (result)
            {
                case HeroActionResult.Success: return "Done!";
                case HeroActionResult.InvalidHero: return "Unknown hero.";
                case HeroActionResult.AlreadyUnlocked: return "Already unlocked.";
                case HeroActionResult.NotUnlocked: return "Unlock this hero first.";
                case HeroActionResult.NotEnoughCurrency:
                    return hero != null && hero.UnlockCurrency == CurrencyType.Gems && !IsUnlocked(hero)
                        ? "Not enough gems." : "Not enough coins.";
                case HeroActionResult.DojoLevelTooLow:
                    return hero != null ? $"Requires Dojo level {hero.RequiredDojoLevel}." : "Upgrade the Dojo first.";
                case HeroActionResult.LevelCapReached:
                    return hero != null
                        ? $"Upgrade the Dojo to level {HeroRules.GetRequiredDojoLevel(GetLevel(hero) + 1)} to raise the level cap."
                        : "Upgrade the Dojo to raise the level cap.";
                case HeroActionResult.MaxLevel: return "Max level reached.";
                default: return result.ToString();
            }
        }

        private static bool Spend(CurrencyType type, int amount)
        {
            if (amount <= 0) return true;
            if (EconomyManager.Instance != null) return EconomyManager.Instance.TrySpend(type, amount);

            // No EconomyManager (tests / unusual boot order): spend on the save's wallet directly.
            bool ok = SaveService.Data.Wallet.TrySpend(type, amount);
            if (ok) SaveService.SaveNow();
            return ok;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            // Persistent: scene loads clear normal subscriptions. Re-grants the starter hero after
            // Settings → Reset or a cloud save replacing the local one.
            EventBus<SaveLoadedEvent>.UnsubscribePersistent(OnSaveLoaded);
            EventBus<SaveLoadedEvent>.SubscribePersistent(OnSaveLoaded);
        }

        private static void OnSaveLoaded(SaveLoadedEvent evt) => EnsureDefaults(evt.Data);
    }
}
