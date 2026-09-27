using System;
using NinjaVillage.Gameplay.Bosses;
using NinjaVillage.Gameplay.Combat;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// NEXT_STEPS.md Part 1-A, automated: the Spider Queen, Shadow Ninja and Nine-Tailed Fox prefabs were
    /// created with the base BossController as a placeholder. This swaps each to its real boss script in
    /// place (keeping every shared serialized value), assigns the player layer mask, the Spider Queen's
    /// spiderling prefab, and a Shadow Ninja projectile prefab. Idempotent.
    /// </summary>
    public static class BossPrefabFixer
    {
        [ContentGenerator("Boss prefabs: real boss scripts + references", 70)]
        public static void Generate()
        {
            int playerMask = LayerMask.GetMask("Player");
            var spider = AssetDatabase.LoadAssetAtPath<GameObject>($"{ContentGen.PrefabRoot}/Spider.prefab");
            var shadowProjectile = EnsureShadowProjectile();

            Fix<SpiderQueenBoss>("SpiderQueen", b => ContentGen.Set(b, ("spiderlingPrefab", spider), ("playerMask", playerMask)));
            Fix<ShadowNinjaBoss>("ShadowNinja", b => ContentGen.Set(b, ("shadowProjectilePrefab", shadowProjectile), ("playerMask", playerMask)));
            Fix<NineTailedFoxBoss>("NineTailedFox", b => ContentGen.Set(b, ("playerMask", playerMask)));
        }

        private static void Fix<T>(string prefabName, Action<T> configure) where T : BossController
        {
            string path = $"{ContentGen.PrefabRoot}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning($"[BossPrefabFixer] {path} not found.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var boss = root.GetComponent<BossController>();
                if (boss != null && boss.GetType() == typeof(BossController))
                {
                    // Re-point the component at the specific script: serialized fields with the same names
                    // (definition, animator, phase thresholds...) keep their values.
                    var script = FindScript(typeof(T));
                    if (script == null) throw new InvalidOperationException($"No MonoScript for {typeof(T).Name}");
                    var so = new SerializedObject(boss);
                    so.FindProperty("m_Script").objectReferenceValue = script;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    PrefabUtility.UnloadPrefabContents(root);
                    root = PrefabUtility.LoadPrefabContents(path);
                }

                var specific = root.GetComponent<T>();
                if (specific == null) throw new InvalidOperationException($"{prefabName} has no {typeof(T).Name} after the swap");
                configure(specific);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static MonoScript FindScript(Type type)
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:MonoScript {type.Name}"))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (script != null && script.GetClass() == type) return script;
            }
            return null;
        }

        /// <summary>A purple copy of the kunai projectile driven by ShadowProjectile, on the Default layer so it can hit the player.</summary>
        private static GameObject EnsureShadowProjectile()
        {
            string path = $"{ContentGen.PrefabRoot}/ShadowProjectile.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var kunai = AssetDatabase.LoadAssetAtPath<GameObject>($"{ContentGen.PrefabRoot}/KunaiProjectile.prefab");
            if (kunai == null || !AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(kunai), path)) return null;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.TryGetComponent<Projectile>(out var playerProjectile)) Object.DestroyImmediate(playerProjectile);
                if (!root.TryGetComponent<ShadowProjectile>(out _)) root.AddComponent<ShadowProjectile>();
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 0; // Default
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true)) renderer.color = new Color(0.55f, 0.25f, 0.9f);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
    }
}
