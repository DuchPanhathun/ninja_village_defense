using System;
using System.Collections.Generic;
using System.Linq;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills;
using NinjaVillage.Gameplay.Ultimates;
using NinjaVillage.Gameplay.Village;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Meta;
using NinjaVillage.UI;
using NinjaVillage.UI.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NinjaVillage.EditorTools
{
    /// <summary>
    /// Builds the MainMenu and Village scenes from code and upgrades Battle.unity, so the whole core
    /// loop (Main Menu → Village → Battle → back) exists without hand-wiring:
    /// <list type="bullet">
    /// <item>MainMenu / Village are regenerated from scratch every run (they contain only a camera, the
    /// village map, and every <see cref="UIScreen"/> tagged with <see cref="SceneScreenAttribute"/>).</item>
    /// <item>Battle.unity is edited in place and idempotently: RunBootstrapper, combined keyboard + touch
    /// joystick input, a default ultimate, and every skill asset in the level-up pool.</item>
    /// <item>Build Profiles scene list: MainMenu (first), Village, Battle.</item>
    /// </list>
    /// </summary>
    public static class SceneBuilder
    {
        private const string SceneFolder = "Assets/_Project/Scenes";
        public static string ScenePath(string sceneName) => $"{SceneFolder}/{sceneName}.unity";

        [MenuItem("Ninja Village/Build Scenes", priority = 1)]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildMainMenu();
            BuildVillage();
            UpgradeBattle();
            UpdateBuildSettings();
            EditorSceneManager.OpenScene(ScenePath(SceneNames.MainMenu), OpenSceneMode.Single);
            Debug.Log("[SceneBuilder] MainMenu, Village and Battle are ready.");
        }

        // ------------------------------------------------------------------ Main Menu

        public static void BuildMainMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Color(0.07f, 0.08f, 0.11f), 6f);
            AddScreens(SceneNames.MainMenu);
            EditorSceneManager.SaveScene(scene, ScenePath(SceneNames.MainMenu));
        }

        // ------------------------------------------------------------------ Village

        public static void BuildVillage()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = CreateCamera(new Color(0.18f, 0.28f, 0.16f), 7f);
            camera.gameObject.AddComponent<VillageCameraController>();
            new GameObject("VillageMap").AddComponent<VillageMap>();
            AddScreens(SceneNames.Village);
            EditorSceneManager.SaveScene(scene, ScenePath(SceneNames.Village));
        }

        private static Camera CreateCamera(Color background, float size)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            go.AddComponent<AudioListener>();
            return camera;
        }

        /// <summary>Every non-abstract UIScreen tagged for <paramref name="sceneName"/>, on one "Screens" object.</summary>
        private static void AddScreens(string sceneName)
        {
            var screens = new GameObject("Screens");
            var types = TypeCache.GetTypesWithAttribute<SceneScreenAttribute>()
                .Where(t => !t.IsAbstract && typeof(UIScreen).IsAssignableFrom(t))
                .Where(t => ((SceneScreenAttribute)Attribute.GetCustomAttribute(t, typeof(SceneScreenAttribute))).Scenes.Contains(sceneName))
                .OrderBy(t => t.Name);
            foreach (var type in types)
                screens.AddComponent(type);
        }

        // ------------------------------------------------------------------ Battle

        public static void UpgradeBattle()
        {
            string path = ScenePath(SceneNames.Battle);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            var controller = Object.FindAnyObjectByType<PlayerController>();
            if (controller == null)
            {
                Debug.LogError("[SceneBuilder] Battle.unity has no PlayerController — skipped Battle upgrade.");
                return;
            }
            var player = controller.gameObject;

            if (!player.TryGetComponent<RunBootstrapper>(out _)) player.AddComponent<RunBootstrapper>();
            if (!player.TryGetComponent<DashController>(out _)) player.AddComponent<DashController>();
            // Evolutions: the manager loads every recipe from the EvolutionCatalog when its list is empty.
            if (!player.TryGetComponent<NinjaVillage.Systems.Evolution.EvolutionJournal>(out _))
                player.AddComponent<NinjaVillage.Systems.Evolution.EvolutionJournal>();
            if (!player.TryGetComponent<NinjaVillage.Systems.Evolution.EvolutionManager>(out _))
                player.AddComponent<NinjaVillage.Systems.Evolution.EvolutionManager>();

            // Movement: keyboard (Editor/desktop) + on-screen joystick (mobile), whichever is pushed harder.
            EnsureJoystick();
            if (!player.TryGetComponent<CombinedMoveInputProvider>(out var combined))
                combined = player.AddComponent<CombinedMoveInputProvider>();
            ContentGen.Set(controller, ("moveInputSource", combined));

            if (player.TryGetComponent<UltimateController>(out var ultimate) && ultimate.EquippedUltimate == null)
            {
                var dragonSlash = AssetDatabase.LoadAssetAtPath<UltimateDefinition>(
                    $"{ContentGen.DataRoot}/Ultimates/Ultimate_DragonSlash.asset");
                if (dragonSlash != null) ContentGen.Set(ultimate, ("equippedUltimate", dragonSlash));
            }

            if (player.TryGetComponent<SkillManager>(out var skills))
                SyncSkillPool(skills);

            foreach (var hook in BattleSceneHooks)
                hook(scene, player);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Extra idempotent Battle-scene steps registered by feature code (evolutions, dash...).</summary>
        public static readonly List<Action<Scene, GameObject>> BattleSceneHooks = new();

        /// <summary>Adds every level-up skill asset to the pool (evolution results are excluded — they're granted by recipes).</summary>
        private static void SyncSkillPool(SkillManager skills)
        {
            var so = new SerializedObject(skills);
            var pool = so.FindProperty("skillPool");
            var current = new List<SkillDefinition>();
            for (int i = 0; i < pool.arraySize; i++)
                if (pool.GetArrayElementAtIndex(i).objectReferenceValue is SkillDefinition s && s != null) current.Add(s);

            foreach (var skill in ContentGen.FindAll<SkillDefinition>(ContentGen.DataRoot + "/Skills"))
                if (!current.Contains(skill) && IsLevelUpSkill(skill)) current.Add(skill);
            current.RemoveAll(s => !IsLevelUpSkill(s));

            ContentGen.Set(skills, ("skillPool", current));
        }

        /// <summary>Evolution results are granted by recipes, never rolled on level-up.</summary>
        private static bool IsLevelUpSkill(SkillDefinition skill) => skill != null && !skill.IsEvolution;

        /// <summary>A full-width touch zone over the lower screen with a dynamic VirtualJoystick.</summary>
        private static void EnsureJoystick()
        {
            if (Object.FindAnyObjectByType<VirtualJoystick>(FindObjectsInactive.Include) != null) return;

            var canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).FirstOrDefault(c => c.isRootCanvas);
            if (canvas == null)
            {
                Debug.LogWarning("[SceneBuilder] Battle.unity has no Canvas — joystick not added.");
                return;
            }

            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var zone = new GameObject("JoystickZone", typeof(RectTransform), typeof(Image));
            var zoneRect = (RectTransform)zone.transform;
            zoneRect.SetParent(canvas.transform, false);
            zoneRect.SetAsFirstSibling(); // behind every button so they still receive taps
            zoneRect.anchorMin = new Vector2(0f, 0f);
            zoneRect.anchorMax = new Vector2(1f, 0.6f);
            zoneRect.offsetMin = zoneRect.offsetMax = Vector2.zero;
            zone.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f); // invisible but raycastable

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var bgRect = (RectTransform)background.transform;
            bgRect.SetParent(zoneRect, false);
            bgRect.sizeDelta = new Vector2(260f, 260f);
            var bgImage = background.GetComponent<Image>();
            bgImage.sprite = knob;
            bgImage.color = new Color(1f, 1f, 1f, 0.25f);
            bgImage.raycastTarget = false;

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            var handleRect = (RectTransform)handle.transform;
            handleRect.SetParent(bgRect, false);
            handleRect.sizeDelta = new Vector2(120f, 120f);
            var handleImage = handle.GetComponent<Image>();
            handleImage.sprite = knob;
            handleImage.color = new Color(1f, 1f, 1f, 0.75f);
            handleImage.raycastTarget = false;

            var joystick = zone.AddComponent<VirtualJoystick>();
            ContentGen.Set(joystick, ("background", bgRect), ("handle", handleRect), ("canvas", canvas), ("handleRange", 100f), ("dynamicPosition", true));
        }

        // ------------------------------------------------------------------ build settings

        public static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = new[] { SceneNames.MainMenu, SceneNames.Village, SceneNames.Battle }
                .Select(name => new EditorBuildSettingsScene(ScenePath(name), true))
                .ToArray();
        }
    }

    /// <summary>One-click / batch-mode setup: generate all content, then build all scenes.</summary>
    public static class ProjectSetup
    {
        [MenuItem("Ninja Village/Setup Everything (Content + Scenes)", priority = 2)]
        public static void RunAll()
        {
            ContentGen.RunGenerators();
            SceneBuilder.BuildAll();
        }

        /// <summary>For <c>unity run -- -executeMethod NinjaVillage.EditorTools.ProjectSetup.RunAllBatch</c>.</summary>
        public static void RunAllBatch()
        {
            int exitCode = 0;
            try
            {
                if (ContentGen.RunGenerators() > 0) exitCode = 2;
                SceneBuilder.BuildMainMenu();
                SceneBuilder.BuildVillage();
                SceneBuilder.UpgradeBattle();
                SceneBuilder.UpdateBuildSettings();
                AssetDatabase.SaveAssets();
                Debug.Log($"[ProjectSetup] Finished with exit code {exitCode}.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }
    }
}
