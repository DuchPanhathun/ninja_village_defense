using System.Collections.Generic;
using System.Linq;
using NinjaVillage.Gameplay.Village;
using NinjaVillage.Systems.Farm;
using UnityEditor;
using UnityEngine;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// Village activities, Phase A + 1: every kind of storehouse goods (crops now; kitchen and fishing goods
    /// ready for the later phases) and the farm's crops — seed cost, real-time growing time, harvest, castle
    /// unlock and the sprites for each stage on the map. Art comes from import_farm() in
    /// <c>Tools/art_import/import_ninja_adventure.py</c>. Tune the tables here, not the assets.
    /// </summary>
    public static class FarmGenerator
    {
        private const string GoodsFolder = ContentGen.DataRoot + "/Village/Goods";
        private const string CropFolder = ContentGen.DataRoot + "/Village/Crops";
        private const string Icons = "Assets/_Project/Art/Sprites/UI/Icons/";
        private const string Farm = "Assets/_Project/Art/Sprites/Environment/Farm/";

        private static readonly (string id, string name, GoodsCategory category, int sellPrice, string description)[] Goods =
        {
            ("rice", "Rice", GoodsCategory.Crop, 6, "Quick to grow. The base of onigiri and sushi."),
            ("radish", "Radish", GoodsCategory.Crop, 20, "Crunchy white radish."),
            ("carrot", "Carrot", GoodsCategory.Crop, 30, "Sweet and orange."),
            ("beet", "Beet", GoodsCategory.Crop, 60, "Deep red and earthy."),
            ("herbs", "Herbs", GoodsCategory.Crop, 90, "Fragrant mountain herbs."),
            ("tea", "Tea Leaves", GoodsCategory.Crop, 160, "Fine green tea from the terraces."),
            ("onigiri", "Onigiri", GoodsCategory.Meal, 40, "A rice ball wrapped in seaweed."),
            ("yakitori", "Yakitori", GoodsCategory.Meal, 80, "Grilled skewers."),
            ("noodle", "Noodle Bowl", GoodsCategory.Meal, 90, "Hot noodles in broth."),
            ("sushi", "Sushi", GoodsCategory.Meal, 120, "Fresh fish on rice."),
            ("sushi_roll", "Sushi Roll", GoodsCategory.Meal, 150, "Rolled with care."),
            ("fortune_cookie", "Fortune Cookie", GoodsCategory.Meal, 25, "What does fate hold?"),
            ("fish", "Fish", GoodsCategory.Fish, 30, "A river fish."),
            ("shrimp", "Shrimp", GoodsCategory.Fish, 40, "Small but tasty."),
            ("calamari", "Calamari", GoodsCategory.Fish, 50, "Squid rings."),
            ("octopus", "Octopus", GoodsCategory.Fish, 70, "A rare catch."),
            ("honey", "Honey", GoodsCategory.Other, 60, "Golden and sweet."),
            ("meat", "Meat", GoodsCategory.Other, 45, "For grilling."),
            ("nut", "Nuts", GoodsCategory.Other, 15, "A handful of nuts."),
        };

        // id, name, seed cost, grow minutes, harvest amount, castle level
        private static readonly (string id, string name, int seedCost, float minutes, int yield, int castle)[] Crops =
        {
            ("rice", "Rice", 10, 5f, 3, 1),
            ("radish", "Radish", 30, 30f, 3, 1),
            ("carrot", "Carrot", 60, 60f, 4, 2),
            ("beet", "Beet", 120, 120f, 4, 3),
            ("herbs", "Herbs", 200, 240f, 5, 4),
            ("tea", "Tea", 300, 480f, 5, 5),
        };

        [ContentGenerator("Farm: goods & crops", 87)]
        public static void Generate()
        {
            var goods = new Dictionary<string, GoodsDefinition>();
            int order = 0;
            foreach (var (id, name, category, price, description) in Goods)
            {
                var definition = ContentGen.CreateOrLoad<GoodsDefinition>($"{GoodsFolder}/Goods_{id}.asset");
                ContentGen.Set(definition, ("id", id), ("displayName", name), ("description", description),
                    ("icon", Sprite(Icons + "item_" + id)), ("category", (int)category), ("sellPrice", price), ("sortOrder", order++));
                goods[id] = definition;
            }
            ContentGen.CreateOrLoad<GoodsCatalog>($"{ContentGen.CatalogRoot}/GoodsCatalog.asset").EditorSetItems(goods.Values);

            var crops = new List<CropDefinition>();
            for (int i = 0; i < Crops.Length; i++)
            {
                var (id, name, cost, minutes, yield, castle) = Crops[i];
                var crop = ContentGen.CreateOrLoad<CropDefinition>($"{CropFolder}/Crop_{id}.asset");
                ContentGen.Set(crop, ("id", id), ("displayName", name), ("icon", Sprite(Icons + "item_" + id)),
                    ("description", goods.TryGetValue(id, out var g) ? g.Description : ""),
                    ("harvest", g), ("harvestAmount", yield), ("seedCost", cost), ("growSeconds", minutes * 60f),
                    ("requiredCastleLevel", castle),
                    ("seedSprite", Sprite(Farm + $"farm_seed_{i % 3}")), ("growingSprite", Sprite(Farm + "farm_growing")),
                    ("ripeSprite", Sprite(Farm + "farm_crop_" + id)));
                crops.Add(crop);
            }
            ContentGen.CreateOrLoad<CropCatalog>($"{ContentGen.CatalogRoot}/CropCatalog.asset").EditorSetItems(crops);

            var art = ContentGen.CreateOrLoad<VillageArt>($"{ContentGen.CatalogRoot}/VillageArt.asset");
            ContentGen.Set(art, ("farmSoil", Sprite(Farm + "farm_soil")),
                ("farmSign", Sprite("Assets/_Project/Art/Sprites/Environment/Decor/deco_signpost")));
            AssetDatabase.SaveAssets();
            Debug.Log($"[Farm] {goods.Count} goods, {crops.Count} crops.");
        }

        private static Sprite Sprite(string pathWithoutExtension)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pathWithoutExtension + ".png");
            if (sprite == null) Debug.LogWarning($"[Farm] Missing sprite {pathWithoutExtension}.png");
            return sprite;
        }
    }
}
