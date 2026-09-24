using System.Collections.Generic;
using System.Linq;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Requests;
using NinjaVillage.Systems.Village;
using UnityEditor;
using UnityEngine;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// Village activities, Phase 3: the favours villagers ask for — deliveries from the storehouse, things to do
    /// in battle or around the village, and clearing the next chapter — with who likes to ask, when they can come
    /// up (castle / kitchen level) and what they pay. Runs after the farm and kitchen generators (goods must
    /// exist). Tune the table here, not the assets.
    /// </summary>
    public static class RequestGenerator
    {
        private const string Folder = ContentGen.DataRoot + "/Village/Requests";

        private readonly struct Row
        {
            public readonly string Id, Title, Line, Goods, Stat, Decoration;
            public readonly RequestKind Kind;
            public readonly int Amount, Castle, Kitchen, Coins, Gems;
            public readonly float Weight;
            public readonly string[] Villagers;

            public Row(string id, RequestKind kind, string title, int amount, string goods, string stat, int castle, int kitchen,
                float weight, int coins, int gems, string decoration, string line, params string[] villagers)
            {
                Id = id; Kind = kind; Title = title; Amount = amount; Goods = goods; Stat = stat; Castle = castle; Kitchen = kitchen;
                Weight = weight; Coins = coins; Gems = gems; Decoration = decoration; Line = line; Villagers = villagers;
            }
        }

        private const string Bring = "Bring {0} {1}";

        private static readonly Row[] Rows =
        {
            // Deliveries from the storehouse
            new("bring_rice", RequestKind.Deliver, Bring, 6, "rice", null, 1, 0, 1.2f, 80, 0, null,
                "The temple's rice jar is empty. Could you spare some?", "npc_villager", "npc_oldwoman"),
            new("bring_radish", RequestKind.Deliver, Bring, 4, "radish", null, 1, 0, 1f, 150, 0, null,
                "I'm pickling radishes for winter! Bring me a few?", "npc_oldwoman", "npc_woman"),
            new("bring_carrot", RequestKind.Deliver, Bring, 4, "carrot", null, 2, 0, 1f, 220, 0, null,
                "Fresh carrots keep a ninja's eyes sharp. Can you bring some?", "npc_villager3", "npc_boy"),
            new("bring_beet", RequestKind.Deliver, Bring, 3, "beet", null, 3, 0, 0.9f, 320, 0, "banner_red",
                "Beets make the best red dye for our banners.", "npc_villager4", "npc_woman"),
            new("bring_herbs", RequestKind.Deliver, Bring, 3, "herbs", null, 4, 0, 0.8f, 450, 2, null,
                "My old bones ache. Mountain herbs would help.", "npc_oldman", "npc_master"),
            new("bring_tea", RequestKind.Deliver, "Bring {0} {1}", 2, "tea", null, 5, 0, 0.8f, 550, 3, "tall_vase",
                "A master deserves proper tea. Would you bring some?", "npc_master"),
            new("bring_onigiri", RequestKind.Deliver, Bring, 2, "onigiri", null, 2, 1, 1f, 180, 0, null,
                "Training all day makes me hungry! Onigiri, please!", "npc_boy", "npc_villager2"),
            new("bring_noodle", RequestKind.Deliver, "Bring a {1}", 1, "noodle", null, 2, 1, 0.9f, 200, 0, null,
                "Nothing beats hot noodles on a cold evening.", "npc_oldman", "npc_villager4"),
            new("bring_yakitori", RequestKind.Deliver, Bring, 2, "yakitori", null, 2, 2, 0.8f, 330, 0, "stump_table",
                "We're having a cookout tonight. Bring yakitori!", "npc_villager2", "npc_villager3"),
            new("bring_sushi_roll", RequestKind.Deliver, "Bring a {1}", 1, "sushi_roll", null, 3, 3, 0.7f, 380, 2, null,
                "I promised my family sushi rolls for the festival.", "npc_woman"),

            // Things to do
            new("defeat_enemies", RequestKind.Stat, "Defeat {0} enemies", 150, null, ProgressStatIds.EnemiesKilled, 1, 0, 1.2f, 150, 0, null,
                "Bandits keep sneaking around the forest. Thin them out for us!", "npc_villager2", "npc_master"),
            new("defeat_bosses", RequestKind.Stat, "Defeat {0} bosses", 2, null, ProgressStatIds.BossesKilled, 1, 0, 0.9f, 250, 3, null,
                "The big ones scare the children. Can you deal with them?", "npc_boy", "npc_woman"),
            new("finish_battles", RequestKind.Stat, "Fight {0} battles", 2, null, ProgressStatIds.RunCompleted, 1, 0, 1f, 120, 0, null,
                "Show the village you're still sharp. Go fight!", "npc_master", "npc_villager"),
            new("harvest_crops", RequestKind.Stat, "Harvest {0} crops", 12, null, ProgressStatIds.CropsHarvested, 1, 0, 1f, 150, 0, "hay_bale",
                "A good harvest makes a happy village.", "npc_villager3", "npc_oldwoman"),
            new("cook_meals", RequestKind.Stat, "Cook {0} meals", 3, null, ProgressStatIds.MealsCooked, 2, 1, 0.9f, 180, 0, null,
                "The whole village smells your cooking. Keep it coming!", "npc_villager4", "npc_oldwoman"),
            new("use_ultimate", RequestKind.Stat, "Use your ultimate {0} times", 3, null, ProgressStatIds.UltimateUsed, 1, 0, 0.8f, 150, 0, null,
                "I heard your ultimate lights up the whole sky. Show me!", "npc_boy"),
            new("open_chests", RequestKind.Stat, "Open {0} treasure chests", 3, null, ProgressStatIds.ChestOpened, 1, 0, 0.8f, 180, 0, "treasure_chest",
                "Legends say the battlefield hides treasure...", "npc_villager4", "npc_boy"),

            // The next chapter
            new("clear_chapter", RequestKind.Chapter, "Clear Chapter {0}", 1, null, null, 1, 0, 0.8f, 400, 10, "lantern_post",
                "Our scouts say a great evil lurks beyond. Clear the way!", "npc_master", "npc_oldman"),
        };

        [ContentGenerator("Villager requests", 90)]
        public static void Generate()
        {
            var goods = ContentGen.FindAll<GoodsDefinition>(ContentGen.DataRoot).Where(g => !string.IsNullOrEmpty(g.Id)).ToDictionary(g => g.Id);
            var decorations = ContentGen.FindAll<DecorationDefinition>(ContentGen.DataRoot).Where(d => !string.IsNullOrEmpty(d.Id)).ToDictionary(d => d.Id);

            var requests = new List<VillagerRequestDefinition>();
            foreach (var row in Rows)
            {
                GoodsDefinition item = null;
                if (row.Goods != null && !goods.TryGetValue(row.Goods, out item)) { Debug.LogWarning($"[Requests] Missing goods '{row.Goods}'."); continue; }
                DecorationDefinition decoration = null;
                if (row.Decoration != null && !decorations.TryGetValue(row.Decoration, out decoration))
                    Debug.LogWarning($"[Requests] Missing decoration '{row.Decoration}'.");

                var request = ContentGen.CreateOrLoad<VillagerRequestDefinition>($"{Folder}/Request_{row.Id}.asset");
                ContentGen.Set(request, ("id", row.Id), ("displayName", row.Title), ("description", row.Line),
                    ("icon", item != null ? item.Icon : null), ("kind", (int)row.Kind), ("goods", item), ("statId", row.Stat ?? ""),
                    ("amount", row.Amount), ("line", row.Line), ("villagers", row.Villagers.Cast<object>().ToList()),
                    ("requiredCastleLevel", row.Castle), ("requiredKitchenLevel", row.Kitchen), ("weight", row.Weight),
                    ("rewardCoins", row.Coins), ("rewardGems", row.Gems), ("rewardDecoration", decoration));
                requests.Add(request);
            }
            ContentGen.CreateOrLoad<VillagerRequestCatalog>($"{ContentGen.CatalogRoot}/VillagerRequestCatalog.asset").EditorSetItems(requests);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Requests] {requests.Count} villager requests.");
        }
    }
}
