using System.Collections.Generic;
using System.Linq;
using NinjaVillage.Gameplay.Chapters;
using NinjaVillage.Gameplay.Enemies;
using NinjaVillage.Gameplay.Waves;
using NinjaVillage.Gameplay.World;
using NinjaVillage.Systems.Chapters;
using NinjaVillage.UI.Battle;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// Battle chapters: five maps (ground, props, sky) with their own enemy rosters, 10 waves each — a
    /// mid-boss partway and the final boss on the last wave — plus the chapter catalog. Also names every
    /// enemy (display names were blank) and tunes the Giant Oni, which still had placeholder stats, into a
    /// real final boss. In the Battle scene it adds the <see cref="ChapterDirector"/> and the HUD's wave
    /// roadmap and minimap. Waves are regenerated on every run; edit this table, not the assets.
    /// </summary>
    public static class ChapterGenerator
    {
        private const string Folder = ContentGen.DataRoot + "/Chapters";
        private const string Props = "Assets/_Project/Art/Sprites/Environment/Village/";
        private const string Grounds = "Assets/_Project/Art/Sprites/Environment/Backgrounds/";
        private const int WavesPerChapter = 10;

        private static readonly (string id, string name)[] EnemyNames =
        {
            ("Bandit", "Bandit"), ("Wolf", "Wolf"), ("Spider", "Spider"), ("Ghost", "Ghost"),
            ("SkeletonNinja", "Skeleton Ninja"), ("Oni", "Oni"), ("GiantOni", "Giant Oni"), ("CursedSamurai", "Cursed Samurai"),
            ("Giant", "Stone Giant"), ("Dragon", "Dragon"), ("ShadowNinja", "Shadow Ninja"), ("NineTailedFox", "Nine-Tailed Fox"),
            ("SpiderQueen", "Spider Queen"), ("DemonKing", "Demon King"),
        };

        private sealed class Chapter
        {
            public int Number;
            public string Name, Description, Ground;
            public Color Sky, Theme;
            public string[] Props;
            public float Density, Difficulty;
            public string[] Enemies;
            /// <summary>wave number → boss id (the last wave's boss is the final boss).</summary>
            public (int wave, string boss)[] Bosses;
            public int Coins, Gems;
        }

        private static readonly Chapter[] Chapters =
        {
            new()
            {
                Number = 1, Name = "Village Outskirts", Ground = "bg_ground_grass",
                Description = "Bandits and wolves raid the fields around the village.",
                Sky = new Color(0.29f, 0.45f, 0.2f), Theme = new Color(0.55f, 0.85f, 0.35f),
                Props = new[] { "prop_tree_green", "prop_roundtree_green", "prop_bush_a", "prop_bush_b", "prop_bush_c", "prop_bush_d", "prop_stump", "prop_rocks_grey", "prop_tree_light" },
                Density = 0.3f, Difficulty = 1f, Enemies = new[] { "Bandit", "Wolf" },
                Bosses = new[] { (5, "CursedSamurai"), (10, "GiantOni") }, Coins = 400, Gems = 20,
            },
            new()
            {
                Number = 2, Name = "Haunted Forest", Ground = "bg_ground_grass_dark",
                Description = "A dark wood where spiders nest and restless spirits drift between the trees.",
                Sky = new Color(0.16f, 0.26f, 0.16f), Theme = new Color(0.5f, 0.8f, 0.55f),
                Props = new[] { "prop_tree_dead", "prop_tree_pine", "prop_bigtree_pine", "prop_boulder_grey", "prop_stump", "prop_bush_c", "prop_rocks_grey" },
                Density = 0.4f, Difficulty = 1.5f, Enemies = new[] { "Spider", "Ghost", "Wolf" },
                Bosses = new[] { (5, "Giant"), (10, "SpiderQueen") }, Coins = 600, Gems = 25,
            },
            new()
            {
                Number = 3, Name = "Scorched Dunes", Ground = "bg_ground_sand",
                Description = "Skeleton ninjas guard the desert road to the ruined shrine.",
                Sky = new Color(0.62f, 0.52f, 0.3f), Theme = new Color(1f, 0.78f, 0.35f),
                Props = new[] { "prop_rocks_brown", "prop_boulder_brown", "prop_tree_dead", "prop_statue_guardian", "prop_stone_arch", "prop_stump_orange" },
                Density = 0.22f, Difficulty = 2.1f, Enemies = new[] { "SkeletonNinja", "Bandit", "Spider" },
                Bosses = new[] { (5, "CursedSamurai"), (10, "ShadowNinja") }, Coins = 800, Gems = 30,
            },
            new()
            {
                Number = 4, Name = "Frozen Peaks", Ground = "bg_ground_snow",
                Description = "Wolves hunt through the snow, and something with nine tails waits at the summit.",
                Sky = new Color(0.62f, 0.7f, 0.78f), Theme = new Color(0.7f, 0.88f, 1f),
                Props = new[] { "prop_tree_snow", "prop_tree_pine_snow", "prop_bigtree_snow", "prop_rocks_grey", "prop_boulder_grey" },
                Density = 0.32f, Difficulty = 2.8f, Enemies = new[] { "Wolf", "Ghost", "SkeletonNinja" },
                Bosses = new[] { (5, "Dragon"), (10, "NineTailedFox") }, Coins = 1000, Gems = 40,
            },
            new()
            {
                Number = 5, Name = "Demon Gate", Ground = "bg_ground_dirt",
                Description = "The oni horde pours out of the demon realm. Close the gate for good.",
                Sky = new Color(0.3f, 0.16f, 0.14f), Theme = new Color(1f, 0.42f, 0.35f),
                Props = new[] { "prop_tree_dead", "prop_rocks_brown", "prop_boulder_brown", "prop_statue_frog", "prop_tree_autumn", "prop_stump_orange" },
                Density = 0.28f, Difficulty = 3.6f, Enemies = new[] { "Oni", "SkeletonNinja", "Ghost" },
                Bosses = new[] { (4, "Giant"), (7, "Dragon"), (10, "DemonKing") }, Coins = 1500, Gems = 60,
            },
        };

        [ContentGenerator("Chapters: maps, waves & bosses", 85)]
        public static void Generate()
        {
            var enemies = ContentGen.FindAll<EnemyDefinition>(ContentGen.DataRoot + "/Enemies").ToDictionary(e => e.Id);
            NameEnemies(enemies);
            TuneGiantOni(enemies);

            var chapters = new List<ChapterDefinition>();
            foreach (var data in Chapters)
            {
                string folder = $"{Folder}/Chapter{data.Number:00}";
                var waves = new List<WaveDefinition>();
                for (int w = 1; w <= WavesPerChapter; w++)
                    waves.Add(BuildWave($"{folder}/Wave{w:00}.asset", data, w, enemies));

                var chapter = ContentGen.CreateOrLoad<ChapterDefinition>($"{folder}/Chapter{data.Number:00}.asset");
                ContentGen.Set(chapter,
                    ("id", $"chapter_{data.Number:00}"), ("displayName", data.Name), ("description", data.Description),
                    ("number", data.Number), ("waves", waves), ("difficulty", data.Difficulty),
                    ("ground", Load(Grounds + data.Ground)), ("groundTint", Color.white), ("skyColor", data.Sky),
                    ("props", data.Props.Select(p => (object)Load(Props + p)).Where(s => s != null).ToList()),
                    ("propDensity", data.Density), ("themeColor", data.Theme),
                    ("clearCoins", data.Coins), ("clearGems", data.Gems));
                chapters.Add(chapter);
            }

            var catalog = ContentGen.CreateOrLoad<ChapterCatalog>($"{ContentGen.CatalogRoot}/ChapterCatalog.asset");
            catalog.EditorSetItems(chapters);
            AssetDatabase.SaveAssets();
        }

        private static Sprite Load(string pathWithoutExtension)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pathWithoutExtension + ".png");
            if (sprite == null) Debug.LogWarning($"[ChapterGenerator] Missing sprite {pathWithoutExtension}.png");
            return sprite;
        }

        /// <summary>
        /// Wave w of 10: the chapter's first enemy throughout, the second from wave 2, the third from wave 4,
        /// each growing per wave; boss waves bring fewer minions plus the boss.
        /// </summary>
        private static WaveDefinition BuildWave(string path, Chapter data, int w, Dictionary<string, EnemyDefinition> enemies)
        {
            var wave = ContentGen.CreateOrLoad<WaveDefinition>(path);
            string bossId = data.Bosses.Where(b => b.wave == w).Select(b => b.boss).FirstOrDefault();
            EnemyDefinition boss = bossId != null && enemies.TryGetValue(bossId, out var b) ? b : null;
            if (bossId != null && boss == null) Debug.LogWarning($"[ChapterGenerator] Unknown boss '{bossId}'.");

            int[] counts = { 6 + 2 * w, w >= 2 ? 2 + w : 0, w >= 4 ? w - 1 : 0 };
            float minionShare = boss == null ? 1f : w == WavesPerChapter ? 0.8f : 0.5f;

            var so = new SerializedObject(wave);
            var spawns = so.FindProperty("spawns");
            spawns.arraySize = 0;
            for (int i = 0; i < data.Enemies.Length && i < counts.Length; i++)
            {
                int count = Mathf.CeilToInt(counts[i] * minionShare);
                if (count <= 0 || !enemies.TryGetValue(data.Enemies[i], out var enemy)) continue;
                spawns.arraySize++;
                var entry = spawns.GetArrayElementAtIndex(spawns.arraySize - 1);
                entry.FindPropertyRelative("EnemyDefinition").objectReferenceValue = enemy;
                entry.FindPropertyRelative("Count").intValue = count;
            }
            so.FindProperty("spawnInterval").floatValue = Mathf.Max(0.25f, 0.55f - 0.03f * w);
            so.FindProperty("isBossWave").boolValue = boss != null;
            so.FindProperty("bossDefinition").objectReferenceValue = boss;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wave);
            return wave;
        }

        /// <summary>Display names for the roadmap, banners and chapter cards (only where still blank).</summary>
        private static void NameEnemies(Dictionary<string, EnemyDefinition> enemies)
        {
            foreach (var (id, name) in EnemyNames)
                if (enemies.TryGetValue(id, out var enemy) && string.IsNullOrEmpty(enemy.DisplayName))
                    ContentGen.Set(enemy, ("displayName", name));
        }

        /// <summary>The Giant Oni was never balanced (30 HP, like a basic enemy); make it a chapter-ending boss.</summary>
        private static void TuneGiantOni(Dictionary<string, EnemyDefinition> enemies)
        {
            if (!enemies.TryGetValue("GiantOni", out var oni) || oni.MaxHealth >= 100f) return;
            ContentGen.Set(oni, ("maxHealth", 420f), ("damage", 22f), ("moveSpeed", 1.7f), ("xpReward", 60), ("coinReward", 20));
        }

        // ------------------------------------------------------------------ Battle scene

        [InitializeOnLoadMethod]
        private static void RegisterBattleHook() => SceneBuilder.BattleSceneHooks.Add(SetUpBattle);

        private static void SetUpBattle(Scene scene, GameObject player)
        {
            var director = Object.FindAnyObjectByType<ChapterDirector>(FindObjectsInactive.Include);
            if (director == null) director = new GameObject("ChapterDirector").AddComponent<ChapterDirector>();
            var props = director.GetComponentInChildren<PropScatter>(true);
            if (props == null)
            {
                props = new GameObject("Props").AddComponent<PropScatter>();
                props.transform.SetParent(director.transform, false);
            }
            var ground = GameObject.Find("Ground");
            ContentGen.Set(director,
                ("waveManager", Object.FindAnyObjectByType<WaveManager>(FindObjectsInactive.Include)),
                ("ground", ground != null ? ground.GetComponent<SpriteRenderer>() : null),
                ("props", props));

            var canvas = FindCanvas(scene, "Canvas");
            if (canvas == null) return;
            var skillBar = canvas.Find("SkillBar");
            int layer = skillBar != null ? skillBar.GetSiblingIndex() + 1 : canvas.childCount;

            // Top row: HP/XP bars on the left, the roadmap between them and the pause button.
            var roadmap = HudChild<WaveRoadmapUI>(canvas, "WaveRoadmap", layer);
            Place(roadmap, new Vector2(1f, 1f), new Vector2(-150f, -24f), new Vector2(410f, 128f));
            // Under the coin counter, clear of the skill bar (left) and the boss bar (below).
            var minimap = HudChild<MinimapUI>(canvas, "Minimap", layer + 1);
            Place(minimap, new Vector2(1f, 1f), new Vector2(-30f, -222f), new Vector2(200f, 200f));
        }

        private static RectTransform FindCanvas(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                    if (canvas.name == name) return (RectTransform)canvas.transform;
            return null;
        }

        private static RectTransform HudChild<T>(RectTransform canvas, string name, int siblingIndex) where T : Component
        {
            var child = canvas.Find(name) as RectTransform;
            if (child == null)
            {
                child = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
                child.SetParent(canvas, false);
            }
            child.SetSiblingIndex(Mathf.Min(siblingIndex, canvas.childCount - 1));
            if (!child.TryGetComponent<T>(out _)) child.gameObject.AddComponent<T>();
            EditorUtility.SetDirty(child.gameObject);
            return child;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            EditorUtility.SetDirty(rt);
        }
    }
}
