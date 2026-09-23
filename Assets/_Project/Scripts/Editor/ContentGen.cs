using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NinjaVillage.EditorTools
{
    /// <summary>
    /// Marks a <c>public static void Method()</c> that creates/updates default content
    /// assets (definitions + catalogs). Every generator runs from
    /// <b>Ninja Village → Generate Default Content</b>, lowest <see cref="Order"/> first.
    /// Generators must be idempotent: re-running updates existing assets in place
    /// (keeping GUIDs, so scene/prefab references survive) instead of duplicating them.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ContentGeneratorAttribute : Attribute
    {
        public int Order { get; }
        public string Name { get; }

        public ContentGeneratorAttribute(string name, int order = 0)
        {
            Name = name;
            Order = order;
        }
    }

    /// <summary>Helpers shared by all content generators.</summary>
    public static class ContentGen
    {
        public const string DataRoot = "Assets/_Project/Data";
        public const string CatalogRoot = "Assets/_Project/Resources/Catalogs";
        public const string PrefabRoot = "Assets/_Project/Prefabs";

        [MenuItem("Ninja Village/Generate Default Content", priority = 0)]
        public static void GenerateAll()
        {
            var generators = TypeCache.GetMethodsWithAttribute<ContentGeneratorAttribute>()
                .Where(m => m.IsStatic && m.GetParameters().Length == 0)
                .Select(m => (method: m, attr: m.GetCustomAttribute<ContentGeneratorAttribute>()))
                .OrderBy(g => g.attr.Order)
                .ThenBy(g => g.attr.Name)
                .ToList();

            // No StartAssetEditing batching on purpose: later generators look up assets that
            // earlier ones created (e.g. hero → weapon), which needs each import to land immediately.
            foreach (var (method, attr) in generators)
            {
                try
                {
                    method.Invoke(null, null);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[ContentGen] ✓ {attr.Name}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ContentGen] ✗ {attr.Name}: {e.InnerException ?? e}");
                }
            }
            AssetDatabase.Refresh();
            Debug.Log($"[ContentGen] Ran {generators.Count} generator(s).");
        }

        /// <summary>Creates every missing folder along <paramref name="assetFolder"/> ("Assets/a/b/c").</summary>
        public static void EnsureFolder(string assetFolder)
        {
            assetFolder = assetFolder.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            string parent = System.IO.Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(assetFolder);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>Loads the asset at <paramref name="assetPath"/> or creates it. Never duplicates.</summary>
        public static T CreateOrLoad<T>(string assetPath) where T : ScriptableObject => (T)CreateOrLoad(typeof(T), assetPath);

        public static ScriptableObject CreateOrLoad(Type type, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath(assetPath, type) as ScriptableObject;
            if (existing != null) return existing;

            EnsureFolder(System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        /// <summary>
        /// Sets serialized (usually private [SerializeField]) fields by name, e.g.
        /// <c>ContentGen.Set(asset, ("id", "fox"), ("displayName", "Fox"), ("maxLevel", 10))</c>.
        /// Supports int, float, bool, string, enums, Color, Vector2/3, Object references,
        /// and arrays/lists of those. Unknown field names log an error instead of failing silently.
        /// </summary>
        public static void Set(Object target, params (string field, object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in values)
            {
                var prop = so.FindProperty(field);
                if (prop == null)
                {
                    Debug.LogError($"[ContentGen] {target.GetType().Name} has no serialized field '{field}'.", target);
                    continue;
                }
                Assign(prop, value);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void Assign(SerializedProperty prop, object value)
        {
            if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
            {
                var list = value as IList ?? Array.Empty<object>();
                prop.arraySize = list.Count;
                for (int i = 0; i < list.Count; i++)
                    Assign(prop.GetArrayElementAtIndex(i), list[i]);
                return;
            }

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: prop.intValue = Convert.ToInt32(value); break;
                case SerializedPropertyType.Float: prop.floatValue = Convert.ToSingle(value); break;
                case SerializedPropertyType.Boolean: prop.boolValue = Convert.ToBoolean(value); break;
                case SerializedPropertyType.String: prop.stringValue = value as string ?? string.Empty; break;
                case SerializedPropertyType.Enum: prop.intValue = Convert.ToInt32(value); break;
                case SerializedPropertyType.Color: prop.colorValue = (Color)value; break;
                case SerializedPropertyType.Vector2: prop.vector2Value = (Vector2)value; break;
                case SerializedPropertyType.Vector3: prop.vector3Value = (Vector3)value; break;
                case SerializedPropertyType.ObjectReference: prop.objectReferenceValue = value as Object; break;
                case SerializedPropertyType.LayerMask: prop.intValue = Convert.ToInt32(value); break;
                default:
                    Debug.LogError($"[ContentGen] Unsupported field type {prop.propertyType} for '{prop.propertyPath}'.");
                    break;
            }
        }

        /// <summary>Finds an existing asset of type <typeparamref name="T"/> by its definition id / asset name.</summary>
        public static T FindAsset<T>(string assetName) where T : Object
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name} {assetName}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && asset.name == assetName) return asset;
            }
            return null;
        }

        /// <summary>All assets of type <typeparamref name="T"/> under <paramref name="folder"/>.</summary>
        public static List<T> FindAll<T>(string folder = "Assets/_Project") where T : Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null)
                .ToList();
    }
}
