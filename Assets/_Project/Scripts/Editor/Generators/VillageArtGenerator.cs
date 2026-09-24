using System.Collections.Generic;
using System.Linq;
using NinjaVillage.Gameplay.Loot;
using NinjaVillage.Gameplay.Village;
using NinjaVillage.Gameplay.Weapons;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using UnityEditor;
using UnityEngine;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// The village in pack pixel art: the <see cref="VillageArt"/> catalog (ground, roads, building sprites
    /// with the Castle's stages, forest, townsfolk, displays), the decoration shop (one
    /// <see cref="DecorationDefinition"/> per row of <see cref="Decorations"/> + the catalog), and icons on
    /// weapons and equipment so the Armory can show them. Sprites come from
    /// <c>Tools/art_import/import_ninja_adventure.py</c> (Environment/Village, Environment/Decor, Characters/Villagers).
    /// </summary>
    public static class VillageArtGenerator
    {
        private const string Sprites = "Assets/_Project/Art/Sprites/";
        private const string DecorFolder = ContentGen.DataRoot + "/Village/Decorations";

        // id, name, category, sprite (Village/ or Decor/ file, or a frame prefix ending in "_"), price, radius, gems
        private static readonly (string id, string name, DecorationCategory category, string sprite, int price, float radius, bool gems)[] Decorations =
        {
            ("sunflower", "Sunflower", DecorationCategory.Nature, "Decor/deco_sunflower", 30, 0.3f, false),
            ("buttercup", "Buttercup", DecorationCategory.Nature, "Decor/deco_yellow_flower", 30, 0.3f, false),
            ("red_blossom", "Red Blossom", DecorationCategory.Nature, "Decor/deco_red_flower", 30, 0.3f, false),
            ("white_lily", "White Lily", DecorationCategory.Nature, "Decor/deco_white_flower", 30, 0.3f, false),
            ("round_bush", "Round Bush", DecorationCategory.Nature, "Village/prop_bush_a", 50, 0.45f, false),
            ("old_stump", "Old Stump", DecorationCategory.Nature, "Village/prop_stump", 60, 0.7f, false),
            ("bamboo", "Bamboo", DecorationCategory.Nature, "Decor/deco_bamboo", 120, 0.35f, false),
            ("cherry_tree", "Cherry Tree", DecorationCategory.Nature, "Village/prop_tree_cherry", 200, 0.9f, false),
            ("maple_tree", "Maple Tree", DecorationCategory.Nature, "Village/prop_tree_autumn", 200, 0.9f, false),
            ("bonsai_tree", "Bonsai Tree", DecorationCategory.Nature, "Village/prop_tree_bonsai", 250, 0.9f, false),
            ("sakura_tree", "Sakura Tree", DecorationCategory.Nature, "Village/prop_roundtree_cherry", 450, 1.2f, false),
            ("grand_sakura", "Grand Sakura", DecorationCategory.Nature, "Village/prop_bigtree_cherry", 900, 1.8f, false),

            ("clay_pot", "Clay Pot", DecorationCategory.Village, "Decor/deco_pot", 50, 0.35f, false),
            ("barrel", "Barrel", DecorationCategory.Village, "Decor/deco_barrel", 60, 0.35f, false),
            ("crate", "Crate", DecorationCategory.Village, "Decor/deco_crate", 60, 0.35f, false),
            ("potted_plant", "Potted Plant", DecorationCategory.Village, "Decor/deco_pot_plant", 70, 0.35f, false),
            ("signpost", "Signpost", DecorationCategory.Village, "Decor/deco_signpost", 80, 0.35f, false),
            ("hay_bale", "Hay Bale", DecorationCategory.Village, "Decor/deco_hay", 90, 0.45f, false),
            ("tall_vase", "Tall Vase", DecorationCategory.Village, "Decor/deco_vase", 150, 0.35f, false),
            ("owl_scarecrow", "Owl Scarecrow", DecorationCategory.Village, "Decor/deco_scarecrow", 180, 0.35f, false),
            ("log_bench", "Log Bench", DecorationCategory.Village, "Decor/deco_log_bench", 180, 1.1f, false),
            ("big_barrel", "Big Barrel", DecorationCategory.Village, "Decor/deco_big_barrel", 200, 0.9f, false),
            ("bench", "Garden Bench", DecorationCategory.Village, "Decor/deco_bench", 220, 1.1f, false),
            ("clothesline", "Clothesline", DecorationCategory.Village, "Decor/deco_clothesline", 260, 1.6f, false),
            ("stump_table", "Stump Table", DecorationCategory.Village, "Decor/deco_stump_table", 280, 0.9f, false),
            ("treasure_chest", "Treasure Chest", DecorationCategory.Village, "Decor/deco_chest", 300, 0.4f, false),
            ("lantern_post", "Lantern Post", DecorationCategory.Village, "Decor/deco_lantern_post", 300, 0.4f, false),
            ("fire_pit", "Fire Pit", DecorationCategory.Village, "Decor/deco_fire_pit", 350, 0.9f, false),
            ("hand_cart", "Hand Cart", DecorationCategory.Village, "Decor/deco_cart", 400, 1f, false),
            ("well", "Well", DecorationCategory.Village, "Decor/deco_well", 500, 0.5f, false),
            ("flower_cart", "Flower Cart", DecorationCategory.Village, "Decor/deco_flower_cart", 550, 1f, false),
            ("camp_tent", "Camp Tent", DecorationCategory.Village, "Decor/deco_tent", 800, 1.5f, false),

            ("stone_pillar", "Stone Pillar", DecorationCategory.Statues, "Decor/deco_stone_pillar", 250, 0.45f, false),
            ("fox_statue", "Fox Statue", DecorationCategory.Statues, "Decor/deco_statue_fox", 600, 0.5f, false),
            ("frog_statue", "Frog Statue", DecorationCategory.Statues, "Village/prop_statue_frog", 700, 0.9f, false),
            ("mossy_frog", "Mossy Frog Statue", DecorationCategory.Statues, "Decor/deco_statue_frog_moss", 800, 0.9f, false),
            ("guardian_idol", "Guardian Idol", DecorationCategory.Statues, "Village/prop_statue_guardian", 900, 1f, false),
            ("monk_statue", "Monk Statue", DecorationCategory.Statues, "Decor/deco_statue_monk", 1000, 0.9f, false),
            ("mossy_monk", "Mossy Monk Statue", DecorationCategory.Statues, "Decor/deco_statue_monk_moss", 1100, 0.9f, false),
            ("orb_monk", "Orb Monk Statue", DecorationCategory.Statues, "Decor/deco_statue_orb_monk", 1200, 0.9f, false),
            ("stone_arch", "Stone Arch", DecorationCategory.Statues, "Village/prop_stone_arch", 1500, 1.4f, false),

            ("banner_red", "Red Banner", DecorationCategory.Banners, "Village/prop_banner_red", 150, 0.4f, false),
            ("banner_orange", "Orange Banner", DecorationCategory.Banners, "Village/prop_banner_orange", 150, 0.4f, false),
            ("banner_green", "Green Banner", DecorationCategory.Banners, "Village/prop_banner_green", 150, 0.4f, false),
            ("banner_purple", "Purple Banner", DecorationCategory.Banners, "Village/prop_banner_purple", 150, 0.4f, false),
            ("banner_yellow", "Yellow Banner", DecorationCategory.Banners, "Village/prop_banner_yellow", 150, 0.4f, false),
            ("banner_white", "White Banner", DecorationCategory.Banners, "Village/prop_banner_white", 150, 0.4f, false),
            ("banner_post", "Banner Post", DecorationCategory.Banners, "Decor/deco_banner_post", 200, 0.4f, false),
            ("flag_red", "Red Flag", DecorationCategory.Banners, "Village/prop_flag_red_", 250, 0.35f, false),
            ("flag_blue", "Blue Flag", DecorationCategory.Banners, "Village/prop_flag_blue_", 250, 0.35f, false),
            ("flag_green", "Green Flag", DecorationCategory.Banners, "Village/prop_flag_green_", 250, 0.35f, false),
            ("flag_yellow", "Yellow Flag", DecorationCategory.Banners, "Village/prop_flag_yellow_", 250, 0.35f, false),

            ("crystal_fire", "Fire Crystal", DecorationCategory.Special, "Decor/deco_crystal_red", 15, 0.35f, true),
            ("crystal_ice", "Ice Crystal", DecorationCategory.Special, "Decor/deco_crystal_blue", 15, 0.35f, true),
            ("crystal_spirit", "Spirit Crystal", DecorationCategory.Special, "Decor/deco_crystal_pink", 15, 0.35f, true),
            ("crystal_jade", "Jade Crystal", DecorationCategory.Special, "Decor/deco_crystal_green", 15, 0.35f, true),
        };

        [ContentGenerator("Village art & decorations", 86)]
        public static void Generate()
        {
            BuildVillageArt();
            BuildDecorations();
            AssignGearIcons();
            AssetDatabase.SaveAssets();
        }

        private static Sprite Sprite(string relative) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Sprites}Environment/{relative}.png");

        private static Sprite[] Frames(string prefix)
        {
            var frames = new List<Sprite>();
            for (int i = 0; ; i++)
            {
                var sprite = Sprite($"{prefix}{i}");
                if (sprite == null) break;
                frames.Add(sprite);
            }
            return frames.ToArray();
        }

        private static void BuildVillageArt()
        {
            var art = ContentGen.CreateOrLoad<VillageArt>($"{ContentGen.CatalogRoot}/VillageArt.asset");
            var buildings = new (string id, string sprite)[]
            {
                (BuildingIds.Dojo, "Village/building_dojo"), (BuildingIds.Forge, "Village/building_forge"),
                (BuildingIds.Market, "Village/building_market"), (BuildingIds.PetHouse, "Village/building_pethouse"),
                (BuildingIds.Shrine, "Village/building_shrine"), (BuildingIds.Kitchen, "Village/building_shop_green"),
            };

            // Castle look per stage name (Hut, House, Manor, Keep, Fortress, Castle), starting at the first
            // level the game's own stage rule gives each stage.
            var castle = VillageService.Catalog != null ? VillageService.Catalog.Get(BuildingIds.Castle) : null;
            string[] stageSprites = { "building_hut_wood", "building_house_orange", "building_house_wood", "building_house_red", "building_castle", "building_castle" };
            var stageLevels = new List<int>();
            var stages = new List<Sprite>();
            string lastStage = null;
            for (int level = 1; castle != null && level <= castle.MaxLevel; level++)
            {
                string stage = castle.StageName(level);
                if (stage == lastStage) continue;
                lastStage = stage;
                stages.Add(Sprite("Village/" + stageSprites[Mathf.Min(stages.Count, stageSprites.Length - 1)]));
                stageLevels.Add(level);
            }

            ContentGen.Set(art,
                ("ground", AssetDatabase.LoadAssetAtPath<Sprite>($"{Sprites}Environment/Backgrounds/bg_ground_grass.png")),
                ("path", AssetDatabase.LoadAssetAtPath<Sprite>($"{Sprites}Environment/Backgrounds/bg_ground_dirt.png")),
                ("buildingIds", buildings.Select(b => (object)b.id).ToList()),
                ("buildingSprites", buildings.Select(b => (object)Sprite(b.sprite)).ToList()),
                ("castleStages", stages.Cast<object>().ToList()),
                ("castleStageLevels", stageLevels.Cast<object>().ToList()),
                ("dojoSign", Sprite("Village/building_dojo_sign")),
                ("flagFrames", Frames("Village/prop_flag_red_").Cast<object>().ToList()),
                ("forestTrees", new[] { "tree_green", "tree_pine", "tree_light", "roundtree_green", "bigtree_green", "bigtree_pine", "tree_cherry" }
                    .Select(n => (object)Sprite("Village/prop_" + n)).Where(s => s != null).ToList()),
                ("forestUndergrowth", new[] { "bush_a", "bush_b", "bush_c", "bush_d", "stump" }
                    .Select(n => (object)Sprite("Village/prop_" + n)).Where(s => s != null).ToList()),
                ("villagerKeys", new[] { "npc_villager", "npc_woman", "npc_oldman", "npc_boy", "npc_villager2", "npc_oldwoman", "npc_villager3", "npc_master", "npc_villager4" }.Cast<object>().ToList()),
                ("animalKeys", new[] { "animal_chicken", "animal_cat", "animal_dog", "animal_pig", "animal_cow", "animal_frog" }.Cast<object>().ToList()),
                ("weaponRack", Sprite("Decor/deco_weapon_rack")),
                ("talentTree", Sprite("Village/prop_bigtree_cherry")),
                ("noticeBoard", Sprite("Decor/deco_bench")));
        }

        private static void BuildDecorations()
        {
            var definitions = new List<DecorationDefinition>();
            foreach (var (id, name, category, spritePath, price, radius, gems) in Decorations)
            {
                var frames = spritePath.EndsWith("_") ? Frames(spritePath) : new[] { Sprite(spritePath) }.Where(s => s != null).ToArray();
                if (frames.Length == 0)
                {
                    Debug.LogWarning($"[VillageArt] Missing sprite for decoration '{id}' ({spritePath}).");
                    continue;
                }
                var definition = ContentGen.CreateOrLoad<DecorationDefinition>($"{DecorFolder}/Decoration_{id}.asset");
                ContentGen.Set(definition,
                    ("id", id), ("displayName", name), ("description", Describe(category)), ("icon", frames[0]),
                    ("category", (int)category), ("frames", frames.Cast<object>().ToList()), ("fps", 6f),
                    ("currency", (int)(gems ? CurrencyType.Gems : CurrencyType.Coins)), ("price", price), ("radius", radius));
                definitions.Add(definition);
            }
            var catalog = ContentGen.CreateOrLoad<DecorationCatalog>($"{ContentGen.CatalogRoot}/DecorationCatalog.asset");
            catalog.EditorSetItems(definitions);
            Debug.Log($"[VillageArt] {definitions.Count} decorations.");
        }

        private static string Describe(DecorationCategory category) => category switch
        {
            DecorationCategory.Nature => "Brings a little nature into the village.",
            DecorationCategory.Statues => "A proud monument for your village.",
            DecorationCategory.Banners => "Show your colours.",
            DecorationCategory.Special => "A rare crystal that glitters in the sun.",
            _ => "Everyday village charm.",
        };

        /// <summary>Weapons and equipment get their pack icons so world displays (the Armory) can show them.</summary>
        private static void AssignGearIcons()
        {
            foreach (var weapon in ContentGen.FindAll<WeaponDefinition>(ContentGen.DataRoot + "/Weapons"))
                SetIcon(weapon, $"icon_weapon_{weapon.Id?.ToLowerInvariant()}");
            foreach (var item in ContentGen.FindAll<EquipmentDefinition>(ContentGen.DataRoot + "/Equipment"))
                SetIcon(item, $"icon_equip_{item.Id}");
        }

        private static void SetIcon(NinjaVillage.Core.ScriptableObjects.DescriptiveScriptableObject definition, string iconName)
        {
            if (definition == null || definition.Icon != null) return;
            var icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{Sprites}UI/Icons/{iconName}.png");
            if (icon != null) ContentGen.Set(definition, ("icon", icon));
        }
    }
}
