using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NinjaVillage.Core.Utilities
{
    /// <summary>Optional hooks for pooled objects to reset their per-use state.</summary>
    public interface IPoolable
    {
        /// <summary>Called every time the instance is taken from the pool (before the caller configures it).</summary>
        void OnSpawned();
        /// <summary>Called when the instance is returned to the pool.</summary>
        void OnDespawned();
    }

    /// <summary>
    /// Object pool for frequently spawned prefabs (EPIC 23 "Performance optimization"): projectiles,
    /// XP orbs, coins and damage numbers are created and destroyed hundreds of times per minute, which
    /// causes GC spikes and frame hitches on mobile. <see cref="Get"/> reuses a deactivated instance of
    /// the same prefab; <see cref="Release"/> deactivates it (or destroys it if it wasn't pooled).
    /// Instances belong to the active scene, so the pool forgets them when the scene unloads.
    /// </summary>
    public static class PrefabPool
    {
        private static readonly Dictionary<GameObject, Stack<GameObject>> Free = new();
        private static readonly Dictionary<GameObject, GameObject> PrefabOf = new();
        private static readonly List<IPoolable> PoolableBuffer = new();

        public static int PooledCount(GameObject prefab) => prefab != null && Free.TryGetValue(prefab, out var s) ? s.Count : 0;

        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            GameObject instance = null;
            if (Free.TryGetValue(prefab, out var stack))
            {
                while (stack.Count > 0 && instance == null)
                    instance = stack.Pop(); // skips instances destroyed behind our back
            }

            if (instance == null)
            {
                instance = Object.Instantiate(prefab, position, rotation);
                PrefabOf[instance] = prefab;
            }
            else
            {
                instance.transform.SetPositionAndRotation(position, rotation);
                instance.SetActive(true);
            }

            instance.GetComponents(PoolableBuffer);
            foreach (var poolable in PoolableBuffer) poolable.OnSpawned();
            return instance;
        }

        public static T Get<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            if (prefab == null) return null;
            var go = Get(prefab.gameObject, position, rotation);
            return go != null ? go.GetComponent<T>() : null;
        }

        /// <summary>Returns an instance to its pool; objects that didn't come from <see cref="Get"/> are destroyed.</summary>
        public static void Release(GameObject instance)
        {
            if (instance == null) return;
            if (!PrefabOf.TryGetValue(instance, out var prefab) || prefab == null)
            {
                Object.Destroy(instance);
                return;
            }
            if (!instance.activeSelf) return; // already released

            instance.GetComponents(PoolableBuffer);
            foreach (var poolable in PoolableBuffer) poolable.OnDespawned();
            instance.SetActive(false);

            if (!Free.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<GameObject>();
                Free[prefab] = stack;
            }
            stack.Push(instance);
        }

        /// <summary>Creates <paramref name="count"/> inactive instances up front (e.g. at battle start).</summary>
        public static void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null) return;
            var created = new List<GameObject>(count);
            for (int i = 0; i < count; i++)
                created.Add(Get(prefab, new Vector3(0f, -9999f, 0f), Quaternion.identity));
            foreach (var go in created) Release(go);
        }

        public static void Clear()
        {
            Free.Clear();
            PrefabOf.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Clear();
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene scene) => Clear();
    }
}
