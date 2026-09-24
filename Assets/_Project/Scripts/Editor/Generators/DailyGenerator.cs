using System.Collections.Generic;
using System.Linq;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Daily;
using NinjaVillage.Systems.Economy;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// Daily content (EPIC 19): the 7-day login calendar, a pool of daily and weekly quests (3 of each
    /// are rolled per period) and tiered achievements. Stat ids are <c>ProgressStatIds</c> values.
    /// </summary>
    public static class DailyGenerator
    {
        private const string QuestFolder = ContentGen.DataRoot + "/Daily/Quests";
        private const string AchievementFolder = ContentGen.DataRoot + "/Daily/Achievements";

        [ContentGenerator("Daily login, quests & achievements", 40)]
        public static void Generate()
        {
            var calendar = ContentGen.CreateOrLoad<LoginCalendar>($"{ContentGen.CatalogRoot}/LoginCalendar.asset", out bool created);
            if (created)
            {
                calendar.EditorSetDays(new[]
                {
                    DailyReward.Coins(100), DailyReward.Coins(150), DailyReward.Gems(5), DailyReward.Coins(250),
                    DailyReward.Coins(300), DailyReward.Gems(10), DailyReward.Gems(30),
                });
            }

            GenerateQuests();
            GenerateAchievements();
        }

        private static void Quest(string id, QuestScope scope, string text, string stat, int target, DailyReward reward, float weight = 1f)
        {
            ContentGen.Define<QuestDefinition>($"{QuestFolder}/{scope}_{id}.asset",
                ("id", $"{scope.ToString().ToLowerInvariant()}_{id}"), ("displayName", text), ("description", text),
                ("scope", scope), ("statId", stat), ("target", target),
                ("reward.currency", reward.currency), ("reward.amount", reward.amount), ("weight", weight));
        }

        private static void GenerateQuests()
        {
            var d = QuestScope.Daily;
            Quest("kill_150", d, "Defeat 150 demons", ProgressStatIds.EnemiesKilled, 150, DailyReward.Coins(120), 1.5f);
            Quest("kill_400", d, "Defeat 400 demons", ProgressStatIds.EnemiesKilled, 400, DailyReward.Coins(220));
            Quest("runs_2", d, "Complete 2 battles", ProgressStatIds.RunCompleted, 2, DailyReward.Coins(100), 1.5f);
            Quest("wave_5", d, "Reach wave 5", ProgressStatIds.WaveReached, 5, DailyReward.Coins(120));
            Quest("survive_300", d, "Survive 5 minutes in one battle", ProgressStatIds.SurvivedSeconds, 300, DailyReward.Coins(150));
            Quest("boss_1", d, "Defeat a boss", ProgressStatIds.BossesKilled, 1, DailyReward.Gems(5), 0.8f);
            Quest("skills_10", d, "Pick 10 skills", ProgressStatIds.SkillPicked, 10, DailyReward.Coins(100));
            Quest("ultimate_2", d, "Use your ultimate 2 times", ProgressStatIds.UltimateUsed, 2, DailyReward.Coins(120));
            Quest("coins_500", d, "Earn 500 coins", ProgressStatIds.CoinsEarned, 500, DailyReward.Gems(3));
            Quest("upgrade_1", d, "Upgrade any building", ProgressStatIds.BuildingUpgraded, 1, DailyReward.Coins(150), 0.8f);
            Quest("spend_800", d, "Spend 800 coins", ProgressStatIds.CoinsSpent, 800, DailyReward.Coins(200), 0.8f);
            Quest("market_1", d, "Buy something at the Market", ProgressStatIds.MarketPurchase, 1, DailyReward.Coins(120), 0.6f);

            var w = QuestScope.Weekly;
            Quest("kill_3000", w, "Defeat 3,000 demons", ProgressStatIds.EnemiesKilled, 3000, DailyReward.Gems(20));
            Quest("bosses_5", w, "Defeat 5 bosses", ProgressStatIds.BossesKilled, 5, DailyReward.Gems(25));
            Quest("wins_3", w, "Win 3 battles", ProgressStatIds.RunVictory, 3, DailyReward.Gems(30));
            Quest("wave_15", w, "Reach wave 15", ProgressStatIds.WaveReached, 15, DailyReward.Gems(25));
            Quest("quests_10", w, "Complete 10 daily quests", ProgressStatIds.QuestCompleted, 10, DailyReward.Gems(30));
            Quest("upgrades_5", w, "Upgrade village buildings 5 times", ProgressStatIds.BuildingUpgraded, 5, DailyReward.Coins(1000));
            Quest("chests_3", w, "Open 3 treasure chests", ProgressStatIds.ChestOpened, 3, DailyReward.Coins(800));

            ContentGen.CreateOrLoad<QuestCatalog>($"{ContentGen.CatalogRoot}/QuestCatalog.asset")
                .EditorSetItems(ContentGen.FindAll<QuestDefinition>(ContentGen.DataRoot).Where(q => !string.IsNullOrEmpty(q.Id))
                    .OrderBy(q => q.Scope).ThenBy(q => q.Id));
        }

        private static int _sort;

        private static void Achievement(string id, string name, string description, string stat, int[] targets, DailyReward[] rewards)
        {
            ContentGen.Define<AchievementDefinition>($"{AchievementFolder}/Achievement_{id}.asset",
                ("id", id), ("displayName", name), ("description", description), ("statId", stat),
                ("tierTargets", targets.Cast<object>().ToList()), ("sortOrder", _sort++));

            // Reward structs (currency + amount per tier) can't go through the flat Set() helper; write them directly.
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<AchievementDefinition>($"{AchievementFolder}/Achievement_{id}.asset");
            var so = new UnityEditor.SerializedObject(asset);
            var list = so.FindProperty("tierRewards");
            if (list.arraySize == 0)
            {
                list.arraySize = rewards.Length;
                for (int i = 0; i < rewards.Length; i++)
                {
                    var element = list.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("currency").intValue = (int)rewards[i].currency;
                    element.FindPropertyRelative("amount").intValue = rewards[i].amount;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                UnityEditor.EditorUtility.SetDirty(asset);
            }
        }

        private static DailyReward[] Gems(params int[] amounts) => amounts.Select(DailyReward.Gems).ToArray();
        private static DailyReward[] Coins(params int[] amounts) => amounts.Select(DailyReward.Coins).ToArray();

        private static void GenerateAchievements()
        {
            _sort = 0;
            Achievement("demon_slayer", "Demon Slayer", "Defeat demons.", ProgressStatIds.EnemiesKilled, new[] { 100, 1000, 10000, 50000 }, Gems(5, 15, 40, 100));
            Achievement("boss_hunter", "Boss Hunter", "Defeat bosses.", ProgressStatIds.BossesKilled, new[] { 1, 10, 50 }, Gems(10, 30, 80));
            Achievement("wave_breaker", "Wave Breaker", "Reach a new best wave.", ProgressStatIds.WaveReached, new[] { 5, 10, 20, 40 }, Gems(5, 15, 30, 60));
            Achievement("survivor", "Survivor", "Survive a long time in one battle (seconds).", ProgressStatIds.SurvivedSeconds, new[] { 300, 600, 1200 }, Gems(10, 25, 50));
            Achievement("veteran", "Veteran", "Complete battles.", ProgressStatIds.RunCompleted, new[] { 5, 25, 100 }, Coins(500, 2000, 8000));
            Achievement("champion", "Champion", "Win battles.", ProgressStatIds.RunVictory, new[] { 1, 10, 50 }, Gems(10, 30, 80));
            Achievement("architect", "Architect", "Upgrade village buildings.", ProgressStatIds.BuildingUpgraded, new[] { 5, 25, 60 }, Coins(500, 2000, 6000));
            Achievement("smith", "Master Smith", "Upgrade weapons at the Forge.", ProgressStatIds.WeaponUpgraded, new[] { 5, 20, 50 }, Coins(400, 1500, 5000));
            Achievement("sensei", "Sensei", "Level up heroes.", ProgressStatIds.HeroUpgraded, new[] { 5, 25, 75 }, Gems(5, 20, 50));
            Achievement("beast_friend", "Beast Friend", "Unlock pets.", ProgressStatIds.PetUnlocked, new[] { 1, 3, 5 }, Gems(5, 15, 40));
            Achievement("scholar", "Forbidden Scholar", "Discover skill evolutions.", ProgressStatIds.EvolutionDiscovered, new[] { 1, 2, 4 }, Gems(15, 30, 60));
            Achievement("treasure", "Treasure Seeker", "Open treasure chests.", ProgressStatIds.ChestOpened, new[] { 1, 10, 50 }, Coins(300, 1500, 6000));
            Achievement("collector", "Collector", "Collect equipment.", ProgressStatIds.EquipmentCollected, new[] { 5, 25, 100 }, Coins(300, 1200, 5000));
            Achievement("loyal", "Loyal Ninja", "Claim daily login rewards.", ProgressStatIds.DailyLoginClaimed, new[] { 7, 30, 100 }, Gems(10, 30, 100));
            Achievement("diligent", "Diligent", "Complete quests.", ProgressStatIds.QuestCompleted, new[] { 10, 50, 200 }, Gems(10, 30, 80));

            ContentGen.CreateOrLoad<AchievementCatalog>($"{ContentGen.CatalogRoot}/AchievementCatalog.asset")
                .EditorSetItems(ContentGen.FindAll<AchievementDefinition>(ContentGen.DataRoot).Where(a => !string.IsNullOrEmpty(a.Id))
                    .OrderBy(a => a.SortOrder));
        }
    }
}
