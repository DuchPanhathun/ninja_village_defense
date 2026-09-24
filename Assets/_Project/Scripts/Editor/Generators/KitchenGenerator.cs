using System.Collections.Generic;
using System.Linq;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Kitchen;
using NinjaVillage.Systems.Talents;
using UnityEditor;
using UnityEngine;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// Village activities, Phase 2 — the Kitchen: which meals boost a battle (and by how much), and the
    /// recipes that cook them from farm crops. Runs after <see cref="FarmGenerator"/> (the goods must exist).
    /// Every meal is worth more than its ingredients, so cooking pays even if you sell. Tune the tables here.
    /// </summary>
    public static class KitchenGenerator
    {
        private const string RecipeFolder = ContentGen.DataRoot + "/Village/Recipes";

        // meal id, boost stat, boost value (talent units: 0.1 = +10%)
        private static readonly (string meal, TalentStat stat, float value)[] Meals =
        {
            ("onigiri", TalentStat.MaxHealth, 0.10f),
            ("noodle", TalentStat.AttackSpeed, 0.10f),
            ("yakitori", TalentStat.AttackDamage, 0.12f),
            ("sushi_roll", TalentStat.XpGain, 0.15f),
            ("fortune_cookie", TalentStat.GoldGain, 0.20f),
        };

        // meal id, cook minutes, kitchen level, ingredients
        private static readonly (string meal, float minutes, int level, (string goods, int amount)[] ingredients)[] Recipes =
        {
            ("onigiri", 2f, 1, new[] { ("rice", 3) }),
            ("noodle", 10f, 1, new[] { ("rice", 2), ("radish", 2) }),
            ("yakitori", 20f, 2, new[] { ("carrot", 2), ("radish", 1) }),
            ("sushi_roll", 30f, 3, new[] { ("rice", 3), ("beet", 1) }),
            ("fortune_cookie", 45f, 4, new[] { ("rice", 2), ("herbs", 1) }),
        };

        [ContentGenerator("Kitchen: meals & recipes", 89)]
        public static void Generate()
        {
            var goods = ContentGen.FindAll<GoodsDefinition>(ContentGen.DataRoot)
                .Where(g => !string.IsNullOrEmpty(g.Id)).ToDictionary(g => g.Id);

            foreach (var (meal, stat, value) in Meals)
            {
                if (!goods.TryGetValue(meal, out var definition)) { Debug.LogWarning($"[Kitchen] Missing goods '{meal}'."); continue; }
                ContentGen.Set(definition, ("mealStat", (int)stat), ("mealValue", value));
            }

            var recipes = new List<RecipeDefinition>();
            foreach (var (meal, minutes, level, ingredients) in Recipes)
            {
                if (!goods.TryGetValue(meal, out var output)) continue;
                var recipe = ContentGen.CreateOrLoad<RecipeDefinition>($"{RecipeFolder}/Recipe_{meal}.asset");
                ContentGen.Set(recipe, ("id", meal), ("displayName", output.DisplayName), ("description", output.Description),
                    ("icon", output.Icon), ("output", output), ("outputAmount", 1), ("cookSeconds", minutes * 60f),
                    ("requiredKitchenLevel", level));

                var so = new SerializedObject(recipe);
                var list = so.FindProperty("ingredients");
                list.arraySize = 0;
                foreach (var (id, amount) in ingredients)
                {
                    if (!goods.TryGetValue(id, out var ingredient)) { Debug.LogWarning($"[Kitchen] Missing ingredient '{id}'."); continue; }
                    list.arraySize++;
                    var entry = list.GetArrayElementAtIndex(list.arraySize - 1);
                    entry.FindPropertyRelative("Goods").objectReferenceValue = ingredient;
                    entry.FindPropertyRelative("Amount").intValue = amount;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(recipe);
                recipes.Add(recipe);
            }
            ContentGen.CreateOrLoad<RecipeCatalog>($"{ContentGen.CatalogRoot}/RecipeCatalog.asset").EditorSetItems(recipes);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Kitchen] {Meals.Length} battle meals, {recipes.Count} recipes.");
        }
    }
}
