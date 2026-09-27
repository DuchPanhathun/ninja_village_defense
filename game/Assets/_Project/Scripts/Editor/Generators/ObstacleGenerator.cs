using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// Solid battle scenery and jumping: adds the "Obstacle" physics layer to the project (the first free
    /// user layer; <see cref="Obstacles"/> sets up its collisions at runtime) and the
    /// <see cref="JumpController"/> to the Battle scene's player.
    /// </summary>
    public static class ObstacleGenerator
    {
        [ContentGenerator("Battle obstacles & jumping", 88)]
        public static void Generate() => EnsureLayer(Obstacles.LayerName);

        /// <summary>Adds <paramref name="name"/> to the first empty user layer (8..31) unless it already exists.</summary>
        public static int EnsureLayer(string name)
        {
            int existing = LayerMask.NameToLayer(name);
            if (existing >= 0) return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var layer = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(layer.stringValue)) continue;
                layer.stringValue = name;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[Obstacles] Added layer '{name}' at index {i}.");
                return i;
            }
            Debug.LogError($"[Obstacles] No free layer for '{name}'.");
            return -1;
        }

        [InitializeOnLoadMethod]
        private static void RegisterBattleHook() => SceneBuilder.BattleSceneHooks.Add(SetUpBattle);

        private static void SetUpBattle(Scene scene, GameObject player)
        {
            if (player != null && !player.TryGetComponent<JumpController>(out _)) player.AddComponent<JumpController>();
        }
    }
}
