using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NinjaVillage.Gameplay.Animation;
using NinjaVillage.Gameplay.Loot;
using NinjaVillage.Gameplay.Progression;
using NinjaVillage.Gameplay.Vfx;
using NinjaVillage.Gameplay.World;
using NinjaVillage.UI.Battle;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// Wires the imported Ninja Adventure art (Tools/art_import/import_ninja_adventure.py) into the
    /// battle. Idempotent — re-run any time:
    /// <list type="bullet">
    /// <item>World sprites get one pixel density: 16 source px (128 texture px) = 1.2 world units.</item>
    /// <item>A <see cref="CharacterSpriteSet"/> per character from the file names
    /// (<c>hero_assassin_run_0..3</c>), plus the <see cref="CharacterSpriteLibrary"/> for heroes, skins, pets.</item>
    /// <item>Enemy/boss prefabs play their frames; their scale becomes 1 with colliders resized so
    /// hitboxes keep their world size. Kunai/shuriken/arrow projectiles, coins and XP gems get sprites.</item>
    /// <item>Battle scene (via <see cref="SceneBuilder.BattleSceneHooks"/>): solid camera background, an
    /// endless grass ground, the default hero's frames on the player, and pack sprites on the HUD bars.</item>
    /// </list>
    /// </summary>
    public static class ArtHookupGenerator
    {
        public const string SpriteRoot = "Assets/_Project/Art/Sprites";
        private const string SetFolder = "Assets/_Project/Data/Art/Characters";
        private const string CatalogFolder = "Assets/_Project/Resources/Catalogs";
        public const float WorldPixelsPerUnit = 128f / 1.2f;
        private static readonly string[] WorldFolders = { "Characters", "Projectiles", "Pickups", "VFX", "Environment" };

        private static readonly Dictionary<string, string> EnemyPrefabs = new()
        {
            ["Bandit"] = "enemy_bandit", ["CursedSamurai"] = "enemy_cursedsamurai", ["Dragon"] = "enemy_dragon",
            ["Ghost"] = "enemy_ghost", ["Giant"] = "enemy_giant", ["Oni"] = "enemy_oni",
            ["SkeletonNinja"] = "enemy_skeletonninja", ["Spider"] = "enemy_spider", ["Wolf"] = "enemy_wolf",
            ["GiantOni"] = "boss_giantoni", ["NineTailedFox"] = "boss_ninetailedfox", ["ShadowNinja"] = "boss_shadowninja",
            ["SpiderQueen"] = "boss_spiderqueen", ["DemonKing"] = "boss_demonking",
        };

        private static readonly Regex FramePattern = new(@"^(.+)_(idle|run|walk|attack|hurt|death)_(\d+)$");

        [ContentGenerator("Art hookup: sprite import, character frames, prefabs", 80)]
        public static void Generate()
        {
            if (!AssetDatabase.IsValidFolder(SpriteRoot))
            {
                Debug.LogWarning($"[ArtHookup] {SpriteRoot} not found — run Tools/art_import/import_ninja_adventure.py first.");
                return;
            }

            ConfigureImporters();
            var sets = BuildCharacterSets();
            BuildLibrary(sets);
            HookEnemyPrefabs(sets);
            HookProjectiles();
            HookPickups();
            BuildPickupArt();
            BuildVfxArt();
        }

        // ------------------------------------------------------------------ import settings

        private static void ConfigureImporters()
        {
            var folders = WorldFolders.Select(f => $"{SpriteRoot}/{f}").Where(AssetDatabase.IsValidFolder).ToArray();
            var changed = new List<string>();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", folders))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;

                    bool ground = Path.GetFileName(path).StartsWith("bg_ground_");
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    bool dirty = false;
                    if (!Mathf.Approximately(settings.spritePixelsPerUnit, WorldPixelsPerUnit))
                    {
                        settings.spritePixelsPerUnit = WorldPixelsPerUnit;
                        dirty = true;
                    }
                    if (ground && settings.spriteMeshType != SpriteMeshType.FullRect)
                    {
                        settings.spriteMeshType = SpriteMeshType.FullRect; // required for tiled drawing
                        dirty = true;
                    }
                    if (!dirty) continue;
                    importer.SetTextureSettings(settings);
                    changed.Add(path);
                    importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            Debug.Log($"[ArtHookup] Import settings updated on {changed.Count} world sprite(s).");
        }

        // ------------------------------------------------------------------ character frames

        private static Dictionary<string, CharacterSpriteSet> BuildCharacterSets()
        {
            var frames = new Dictionary<string, Dictionary<CharacterAnim, SortedDictionary<int, Sprite>>>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { $"{SpriteRoot}/Characters" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var match = FramePattern.Match(Path.GetFileNameWithoutExtension(path));
                if (!match.Success) continue;

                string key = match.Groups[1].Value;
                var anim = match.Groups[2].Value switch
                {
                    "run" or "walk" => CharacterAnim.Move,
                    "attack" => CharacterAnim.Attack,
                    "hurt" => CharacterAnim.Hurt,
                    "death" => CharacterAnim.Death,
                    _ => CharacterAnim.Idle,
                };
                // Enemies and bosses are hit constantly: a hurt clip on every hit would freeze them in
                // it, so only heroes (and their skins) get one. HitFlash covers the rest.
                if (anim == CharacterAnim.Hurt && !(key.StartsWith("hero_") || key.StartsWith("skin_"))) continue;

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;
                if (!frames.TryGetValue(key, out var byAnim)) frames[key] = byAnim = new();
                if (!byAnim.TryGetValue(anim, out var byIndex)) byAnim[anim] = byIndex = new();
                byIndex[int.Parse(match.Groups[3].Value)] = sprite;
            }

            ContentGen.EnsureFolder(SetFolder);
            var sets = new Dictionary<string, CharacterSpriteSet>();
            foreach (var (key, byAnim) in frames.OrderBy(k => k.Key))
            {
                Sprite[] Get(CharacterAnim a) => byAnim.TryGetValue(a, out var d) ? d.Values.ToArray() : System.Array.Empty<Sprite>();
                bool boss = key.StartsWith("boss_");
                var set = ContentGen.CreateOrLoad<CharacterSpriteSet>($"{SetFolder}/{key}.asset");
                ContentGen.Set(set, ("key", key), ("idle", Get(CharacterAnim.Idle)), ("move", Get(CharacterAnim.Move)),
                    ("attack", Get(CharacterAnim.Attack)), ("hurt", Get(CharacterAnim.Hurt)), ("death", Get(CharacterAnim.Death)),
                    ("idleFps", boss ? 8f : 5f), ("moveFps", boss ? 8f : 9f));
                sets[key] = set;
            }
            Debug.Log($"[ArtHookup] {sets.Count} character sprite sets.");
            return sets;
        }

        private static void BuildLibrary(Dictionary<string, CharacterSpriteSet> sets)
        {
            ContentGen.EnsureFolder(CatalogFolder);
            var library = ContentGen.CreateOrLoad<CharacterSpriteLibrary>($"{CatalogFolder}/{nameof(CharacterSpriteLibrary)}.asset");
            library.EditorSetSets(sets.Where(s => s.Key.StartsWith("hero_") || s.Key.StartsWith("skin_") || s.Key.StartsWith("pet_"))
                .Select(s => s.Value).ToArray());
        }

        // ------------------------------------------------------------------ prefabs

        private static void HookEnemyPrefabs(Dictionary<string, CharacterSpriteSet> sets)
        {
            foreach (var (prefabName, key) in EnemyPrefabs)
            {
                if (!sets.TryGetValue(key, out var set)) { Debug.LogWarning($"[ArtHookup] No sprites for {key}."); continue; }
                EditPrefab(prefabName, root =>
                {
                    var renderer = root.GetComponentInChildren<SpriteRenderer>();
                    if (renderer == null) return;
                    renderer.sprite = set.DefaultSprite;
                    renderer.color = Color.white;
                    if (!root.TryGetComponent<SpriteFrameAnimator>(out var animator)) animator = root.AddComponent<SpriteFrameAnimator>();
                    ContentGen.Set(animator, ("spriteSet", set), ("target", renderer));
                    NormalizeScale(root, 1f);
                });
            }
        }

        private static void HookProjectiles()
        {
            EditPrefab("KunaiProjectile", root => Dress(root, Load("Projectiles", "projectile_kunai"), 0.8f));
            EditPrefab("ShurikenProjectile", root =>
            {
                var spin = Frames("Projectiles", "projectile_shuriken");
                Dress(root, spin.FirstOrDefault(), 0.55f);
                Loop(root, spin, 16f);
            });

            // The bow used the kunai prefab; give it a real arrow.
            string arrowPath = $"{ContentGen.PrefabRoot}/ArrowProjectile.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(arrowPath) == null)
                AssetDatabase.CopyAsset($"{ContentGen.PrefabRoot}/KunaiProjectile.prefab", arrowPath);
            EditPrefab("ArrowProjectile", root =>
            {
                root.name = "ArrowProjectile";
                Dress(root, Load("Projectiles", "projectile_arrow"), 0.8f);
            });
            var bow = AssetDatabase.LoadAssetAtPath<ScriptableObject>($"{ContentGen.DataRoot}/Weapons/Bow.asset");
            var arrow = AssetDatabase.LoadAssetAtPath<GameObject>(arrowPath);
            if (bow != null && arrow != null) ContentGen.Set(bow, ("projectilePrefab", arrow));
        }

        private static void HookPickups()
        {
            EditPrefab("Coin", root =>
            {
                var spin = Frames("Pickups", "pickup_coin");
                Dress(root, spin.FirstOrDefault(), 0.6f);
                Loop(root, spin, 8f);
            });
            EditPrefab("XpOrb", root =>
            {
                var small = Load("Pickups", "pickup_xp_small");
                Dress(root, small, 0.45f);
                if (root.TryGetComponent<XpOrb>(out var orb))
                    ContentGen.Set(orb, ("tierSprites", new Object[] { small, Load("Pickups", "pickup_xp_medium"), Load("Pickups", "pickup_xp_large") }));
            });
        }

        private static void BuildPickupArt()
        {
            var art = ContentGen.CreateOrLoad<PickupArt>($"{CatalogFolder}/{nameof(PickupArt)}.asset");
            ContentGen.Set(art, ("chestClosed", Load("Pickups", "pickup_bigchest_0")), ("chestOpen", Load("Pickups", "pickup_bigchest_1")));
        }

        private static void BuildVfxArt()
        {
            var art = ContentGen.CreateOrLoad<VfxArt>($"{CatalogFolder}/{nameof(VfxArt)}.asset");
            ContentGen.Set(art, ("hit", Frames("VFX", "vfx_hit")), ("smoke", Frames("VFX", "vfx_smoke")),
                ("explosion", Frames("VFX", "vfx_explosion")), ("thunder", Frames("VFX", "vfx_thunder")),
                ("slash", Frames("VFX", "vfx_slash_curved")), ("ring", Frames("VFX", "vfx_hit_ring")), ("fps", 20f));
        }

        // ------------------------------------------------------------------ Battle scene

        [InitializeOnLoadMethod]
        private static void RegisterBattleHook() => SceneBuilder.BattleSceneHooks.Add(DressBattle);

        private static void DressBattle(Scene scene, GameObject player)
        {
            var camera = Object.FindAnyObjectByType<UnityEngine.Camera>();
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor; // was Skybox: the sky/horizon behind the battle
                camera.backgroundColor = new Color(0.29f, 0.45f, 0.2f);
                EditorUtility.SetDirty(camera);
            }

            var groundSprite = Load("Environment/Backgrounds", "bg_ground_grass");
            if (groundSprite != null)
            {
                var ground = GameObject.Find("Ground") ?? new GameObject("Ground");
                if (!ground.TryGetComponent<SpriteRenderer>(out var groundRenderer)) groundRenderer = ground.AddComponent<SpriteRenderer>();
                groundRenderer.sprite = groundSprite;
                groundRenderer.drawMode = SpriteDrawMode.Tiled;
                groundRenderer.size = new Vector2(40f, 40f);
                groundRenderer.sortingOrder = -1000;
                if (!ground.TryGetComponent<InfiniteGround>(out _)) ground.AddComponent<InfiniteGround>();
                EditorUtility.SetDirty(ground);
            }

            // Default look before the run-start modifiers pick the selected hero and skin.
            var heroSet = AssetDatabase.LoadAssetAtPath<CharacterSpriteSet>($"{SetFolder}/hero_assassin.asset");
            var playerRenderer = player.GetComponentInChildren<SpriteRenderer>();
            if (heroSet != null && playerRenderer != null)
            {
                playerRenderer.sprite = heroSet.DefaultSprite;
                playerRenderer.color = Color.white;
                if (!player.TryGetComponent<SpriteFrameAnimator>(out var animator)) animator = player.AddComponent<SpriteFrameAnimator>();
                ContentGen.Set(animator, ("spriteSet", heroSet), ("target", playerRenderer));
            }

            DressBar(Object.FindAnyObjectByType<PlayerHealthBarUI>(), "bar_hp_fill", "HPBarBackground");
            DressBar(Object.FindAnyObjectByType<XpBarUI>(), "bar_xp_fill", "XPBarBackground");
            DressBar(Object.FindAnyObjectByType<BossHealthBarUI>(), "bar_hp_fill", "BossBarRoot");
        }

        private static void DressBar(Component bar, string fillSprite, string backgroundName)
        {
            if (bar == null) return;
            var fill = new SerializedObject(bar).FindProperty("fillImage")?.objectReferenceValue as Image;
            if (fill != null)
            {
                fill.sprite = Load("UI/Bars", fillSprite);
                fill.type = Image.Type.Filled; // fillAmount only works on Filled images; unset it drew a white block
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                fill.color = Color.white;
                EditorUtility.SetDirty(fill);
            }
            var background = GameObject.Find(backgroundName);
            if (background != null && background.TryGetComponent<Image>(out var bg))
            {
                bg.sprite = Load("UI/Bars", "bar_hp_bg");
                bg.type = Image.Type.Simple;
                bg.color = Color.white;
                EditorUtility.SetDirty(bg);
            }
        }

        // ------------------------------------------------------------------ helpers

        private static Sprite Load(string folder, string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteRoot}/{folder}/{name}.png");

        private static Sprite[] Frames(string folder, string prefix)
        {
            var list = new List<Sprite>();
            for (int i = 0; ; i++)
            {
                var sprite = Load(folder, $"{prefix}_{i}");
                if (sprite == null) break;
                list.Add(sprite);
            }
            return list.ToArray();
        }

        private static void EditPrefab(string prefabName, System.Action<GameObject> edit)
        {
            string path = $"{ContentGen.PrefabRoot}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning($"[ArtHookup] {path} not found.");
                return;
            }
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Puts a sprite on a small prefab (projectile, pickup) at the given scale, keeping hitboxes the same size.</summary>
        private static void Dress(GameObject root, Sprite sprite, float scale)
        {
            if (sprite == null) return;
            var renderer = root.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null) return;
            renderer.sprite = sprite;
            renderer.color = Color.white;
            NormalizeScale(root, scale);
        }

        private static void Loop(GameObject root, Sprite[] frames, float fps)
        {
            if (frames.Length < 2) return;
            var renderer = root.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null) return;
            if (!renderer.TryGetComponent<SpriteLoop>(out var loop)) loop = renderer.gameObject.AddComponent<SpriteLoop>();
            ContentGen.Set(loop, ("frames", frames), ("fps", fps));
        }

        /// <summary>
        /// Sets the root's uniform scale (keeping the facing sign) and resizes colliders and child offsets
        /// by the inverse, so hitboxes and attach points keep their world size.
        /// </summary>
        private static void NormalizeScale(GameObject root, float scale)
        {
            var t = root.transform;
            float old = Mathf.Abs(t.localScale.y);
            if (old <= 0f || Mathf.Approximately(old, scale)) return;
            float k = old / scale;

            foreach (var box in root.GetComponents<BoxCollider2D>()) { box.size *= k; box.offset *= k; }
            foreach (var circle in root.GetComponents<CircleCollider2D>()) { circle.radius *= k; circle.offset *= k; }
            foreach (var capsule in root.GetComponents<CapsuleCollider2D>()) { capsule.size *= k; capsule.offset *= k; }
            foreach (Transform child in t)
            {
                child.localPosition *= k;
                child.localScale *= k;
            }
            float sign = t.localScale.x < 0f ? -1f : 1f;
            t.localScale = new Vector3(sign * scale, scale, t.localScale.z);
        }
    }
}
