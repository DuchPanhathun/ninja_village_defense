using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Systems.Save
{
    /// <summary>
    /// The single in-memory copy of the player's <see cref="SaveData"/>, shared by every
    /// scene (Main Menu, Village, Battle). Systems read and mutate <see cref="Data"/>
    /// directly, then call <see cref="MarkDirty"/> (batched write) or <see cref="SaveNow"/>
    /// (immediate write, e.g. after a purchase).
    ///
    /// A hidden DontDestroyOnLoad runner is created automatically before the first scene
    /// loads, so no scene needs to place this — it also flushes on app pause/quit, which is
    /// the only reliable "exit" signal on mobile.
    /// </summary>
    public static class SaveService
    {
        private static SaveData _data;
        private static bool _dirty;

        /// <summary>Loaded lazily on first access.</summary>
        public static SaveData Data
        {
            get
            {
                if (_data == null) Load();
                return _data;
            }
        }

        public static bool IsDirty => _dirty;

        /// <summary>(Re)reads the save from disk and broadcasts <see cref="SaveLoadedEvent"/>.</summary>
        public static void Load()
        {
            _data = SaveSystem.Load();
            _data.Profile.LastSessionUtcTicks = System.DateTime.UtcNow.Ticks;
            _dirty = false;
            EventBus<SaveLoadedEvent>.Raise(new SaveLoadedEvent(_data));
        }

        /// <summary>Swaps in a whole new save (e.g. a newer cloud copy) and persists it.</summary>
        public static void Replace(SaveData data)
        {
            if (data == null) return;
            data.Migrate();
            _data = data;
            SaveNow();
            EventBus<SaveLoadedEvent>.Raise(new SaveLoadedEvent(_data));
        }

        /// <summary>Schedules a write at the end of the frame (coalesces many small mutations).</summary>
        public static void MarkDirty() => _dirty = true;

        public static void SaveNow()
        {
            if (_data == null) return;
            SaveSystem.Save(_data);
            _dirty = false;
            EventBus<SaveWrittenEvent>.Raise(new SaveWrittenEvent(_data));
        }

        /// <summary>Wipes all progress (Settings → Reset). Starts a fresh save immediately.</summary>
        public static void ResetAll()
        {
            SaveSystem.Delete();
            _data = new SaveData();
            _data.Migrate();
            SaveNow();
            EventBus<SaveLoadedEvent>.Raise(new SaveLoadedEvent(_data));
        }

        internal static void FlushIfDirty()
        {
            if (_dirty) SaveNow();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Supports "Enter Play Mode Options" with domain reload disabled.
            _data = null;
            _dirty = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateRunner()
        {
            var go = new GameObject("[SaveService]");
            go.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(go);
            go.AddComponent<SaveServiceRunner>();
        }
    }

    /// <summary>Lifecycle pump for <see cref="SaveService"/> — flushes batched writes and saves on pause/quit.</summary>
    internal sealed class SaveServiceRunner : MonoBehaviour
    {
        private void LateUpdate() => SaveService.FlushIfDirty();

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveService.SaveNow();
        }

        private void OnApplicationQuit() => SaveService.SaveNow();
    }
}
