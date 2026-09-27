using System.Collections.Generic;
using NinjaVillage.Core.ScriptableObjects;
using UnityEngine;

namespace NinjaVillage.Core.Data
{
    /// <summary>
    /// A list of definition assets with lookup by <see cref="DescriptiveScriptableObject.Id"/>
    /// — how save data (which stores ids) is turned back into assets. Each feature makes a
    /// concrete subclass (<c>HeroCatalog : DefinitionCatalog&lt;HeroDefinition&gt;</c>), saves
    /// one instance under <c>Assets/_Project/Resources/Catalogs/</c>, and loads it with
    /// <see cref="CatalogLoader"/>.
    /// </summary>
    public abstract class DefinitionCatalog<T> : ScriptableObject where T : DescriptiveScriptableObject
    {
        [SerializeField] private List<T> items = new();

        private Dictionary<string, T> _byId;

        public IReadOnlyList<T> All => items;

        public T Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            EnsureIndex();
            return _byId.TryGetValue(id, out var def) ? def : null;
        }

        public bool TryGet(string id, out T definition)
        {
            definition = Get(id);
            return definition != null;
        }

        private void EnsureIndex()
        {
            if (_byId != null) return;
            _byId = new Dictionary<string, T>();
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.Id)) continue;
                if (!_byId.TryAdd(item.Id, item))
                    Debug.LogWarning($"{name}: duplicate id '{item.Id}' — ids must be unique.", this);
            }
        }

        protected virtual void OnEnable() => _byId = null;

#if UNITY_EDITOR
        /// <summary>Editor-only: used by content generators to fill the catalog.</summary>
        public void EditorSetItems(IEnumerable<T> newItems)
        {
            items = new List<T>(newItems);
            _byId = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    /// <summary>Loads (and caches) catalogs from <c>Resources/Catalogs/</c>.</summary>
    public static class CatalogLoader
    {
        public const string ResourcesFolder = "Catalogs";

        private static readonly Dictionary<System.Type, ScriptableObject> Cache = new();

        /// <summary>Loads <c>Resources/Catalogs/{typeof(T).Name}</c> — name each catalog asset after its class.</summary>
        public static T Load<T>() where T : ScriptableObject
        {
            if (Cache.TryGetValue(typeof(T), out var cached) && cached != null)
                return (T)cached;

            var catalog = Resources.Load<T>($"{ResourcesFolder}/{typeof(T).Name}");
            if (catalog == null)
                Debug.LogWarning($"CatalogLoader: no '{typeof(T).Name}' asset in Resources/{ResourcesFolder}/. Run Ninja Village → Generate Default Content.");
            else
                Cache[typeof(T)] = catalog;
            return catalog;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => Cache.Clear();
    }
}
