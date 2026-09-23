using System;
using System.Collections.Generic;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Heroes
{
    /// <summary>
    /// The hero system's rules as pure functions over primitives and save sections, so they are
    /// unit-tested without assets (see HeroTests):
    /// <list type="bullet">
    /// <item>Level cap = <see cref="BaseLevelCap"/> + Dojo level × <see cref="LevelsPerDojoLevel"/>, never above the hero's max
    /// (Dojo 0 still allows 5 levels) — this is the Dojo's "Hero upgrade" role.</item>
    /// <item>Upgrade cost L → L+1 = base × L^exponent, rounded to 10 coins.</item>
    /// <item>The starter hero(es) are granted free and a valid hero is always selected.</item>
    /// </list>
    /// </summary>
    public static class HeroRules
    {
        public const int BaseLevelCap = 5;
        public const int LevelsPerDojoLevel = 3;
        public const int CostRounding = 10;

        public static int GetLevelCap(int dojoLevel, int heroMaxLevel)
        {
            int cap = BaseLevelCap + Math.Max(0, dojoLevel) * LevelsPerDojoLevel;
            return Math.Max(1, Math.Min(Math.Max(1, heroMaxLevel), cap));
        }

        /// <summary>Lowest Dojo level whose cap allows <paramref name="targetLevel"/>.</summary>
        public static int GetRequiredDojoLevel(int targetLevel)
        {
            if (targetLevel <= BaseLevelCap) return 0;
            return (targetLevel - BaseLevelCap + LevelsPerDojoLevel - 1) / LevelsPerDojoLevel;
        }

        /// <summary>Coins to go from <paramref name="currentLevel"/> to the next level. 0 when base cost is 0.</summary>
        public static int GetUpgradeCost(int currentLevel, int baseCost, float exponent)
        {
            if (baseCost <= 0) return 0;
            int level = Math.Max(1, currentLevel);
            double raw = baseCost * Math.Pow(level, Math.Max(0f, exponent));
            long rounded = (long)Math.Round(raw / CostRounding, MidpointRounding.AwayFromZero) * CostRounding;
            return (int)Math.Min(int.MaxValue, Math.Max(CostRounding, rounded));
        }

        public static HeroActionResult CheckUnlock(bool alreadyOwned, int dojoLevel, int requiredDojoLevel, int balance, int cost)
        {
            if (alreadyOwned) return HeroActionResult.AlreadyUnlocked;
            if (dojoLevel < requiredDojoLevel) return HeroActionResult.DojoLevelTooLow;
            if (balance < cost) return HeroActionResult.NotEnoughCurrency;
            return HeroActionResult.Success;
        }

        public static HeroActionResult CheckUpgrade(bool owned, int currentLevel, int levelCap, int maxLevel, int balance, int cost)
        {
            if (!owned) return HeroActionResult.NotUnlocked;
            if (currentLevel >= maxLevel) return HeroActionResult.MaxLevel;
            if (currentLevel >= levelCap) return HeroActionResult.LevelCapReached;
            if (balance < cost) return HeroActionResult.NotEnoughCurrency;
            return HeroActionResult.Success;
        }

        /// <summary>
        /// Grants every free starter hero the player doesn't own yet (at level 1) and makes sure the
        /// selected hero is an owned one. Returns true when the save changed.
        /// </summary>
        /// <param name="starterHeroIds">Free heroes in priority order — the first becomes the default selection.</param>
        public static bool EnsureStarterHeroes(HeroSaveData save, IReadOnlyList<string> starterHeroIds)
        {
            if (save == null) return false;
            save.EnsureInitialized();
            bool changed = false;

            if (starterHeroIds != null)
            {
                foreach (var id in starterHeroIds)
                {
                    if (string.IsNullOrEmpty(id) || save.Owned.ContainsId(id)) continue;
                    save.Owned.SetLevel(id, 1);
                    changed = true;
                }
            }

            // Repair entries with an invalid level (hand-edited / corrupted saves).
            foreach (var entry in save.Owned)
            {
                if (entry != null && entry.Level < 1)
                {
                    entry.Level = 1;
                    changed = true;
                }
            }

            if (string.IsNullOrEmpty(save.SelectedHeroId) || !save.Owned.ContainsId(save.SelectedHeroId))
            {
                string fallback = null;
                if (starterHeroIds != null)
                {
                    foreach (var id in starterHeroIds)
                        if (!string.IsNullOrEmpty(id) && save.Owned.ContainsId(id)) { fallback = id; break; }
                }
                if (fallback == null)
                {
                    foreach (var entry in save.Owned)
                        if (entry != null && !string.IsNullOrEmpty(entry.Id)) { fallback = entry.Id; break; }
                }

                if (fallback != null && fallback != save.SelectedHeroId)
                {
                    save.SelectedHeroId = fallback;
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>Beast Ninja pack size: base + one every <paramref name="extraEveryLevels"/> levels, capped.</summary>
        public static int GetCompanionCount(int baseCount, int extraEveryLevels, int heroLevel, int maxCompanions)
        {
            if (baseCount <= 0 && extraEveryLevels <= 0) return 0;
            int count = Math.Max(0, baseCount);
            if (extraEveryLevels > 0) count += Math.Max(0, heroLevel) / extraEveryLevels;
            return Math.Min(count, Math.Max(0, maxCompanions));
        }

        /// <summary>Companion pets level with the hero at half speed: hero 1 → 1, hero 11 → 6, capped at the pet's max.</summary>
        public static int GetCompanionLevel(int heroLevel, int petMaxLevel) =>
            Math.Max(1, Math.Min(Math.Max(1, petMaxLevel), 1 + (Math.Max(1, heroLevel) - 1) / 2));
    }
}
