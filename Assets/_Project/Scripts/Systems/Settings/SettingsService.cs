using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Settings
{
    /// <summary>
    /// Owns applying <see cref="SettingsSaveData"/> to the engine: quality level, target frame
    /// rate (battery saver), and — through <see cref="SettingsChangedEvent"/> — audio volumes.
    /// UI calls the static setters; each one validates, applies live and persists.
    ///
    /// Volume sliders fire every frame while dragged, so volume changes are persisted with a
    /// short debounce instead of a disk write per frame. One hidden DontDestroyOnLoad instance
    /// is created before the first scene; it also re-applies everything after a save reload
    /// (Settings → Reset progress, cloud save replacing the local one).
    /// </summary>
    public class SettingsService : MonoBehaviour
    {
        private const float SaveDebounceSeconds = 0.75f;

        public static SettingsService Instance { get; private set; }

        /// <summary>The live settings section of the save.</summary>
        public static SettingsSaveData Current => SaveService.Data.Settings;

        private float _saveAt = -1f;

        // ------------------------------------------------------------------ setters

        public static void SetMasterVolume(float volume) => SetVolume(ref Current.MasterVolume, volume);
        public static void SetMusicVolume(float volume) => SetVolume(ref Current.MusicVolume, volume);
        public static void SetSfxVolume(float volume) => SetVolume(ref Current.SfxVolume, volume);

        public static void SetVibration(bool enabled)
        {
            Current.Vibration = enabled;
            Commit(immediate: true);
            if (enabled) Vibrate(force: true); // feedback that it's on
        }

        public static void SetShowDamageNumbers(bool show)
        {
            Current.ShowDamageNumbers = show;
            Commit(immediate: true);
        }

        public static void SetScreenShake(bool enabled)
        {
            Current.ScreenShake = enabled;
            NinjaVillage.Gameplay.Camera.CameraShake.Enabled = enabled;
            Commit(immediate: true);
        }

        /// <summary>true = 30 FPS battery saver, false = 60 FPS.</summary>
        public static void SetBatterySaver(bool enabled)
        {
            Current.TargetFrameRate = enabled ? SettingsSaveData.BatterySaverFrameRate : SettingsSaveData.DefaultFrameRate;
            ApplyFrameRate(Current);
            Commit(immediate: true);
        }

        /// <summary>Index into <c>QualitySettings.names</c>; -1 keeps the platform default.</summary>
        public static void SetQualityLevel(int level)
        {
            Current.QualityLevel = SettingsSaveData.ClampQualityLevel(level, QualitySettings.names.Length);
            ApplyQuality(Current);
            Commit(immediate: true);
        }

        /// <summary>Quality level currently in effect (the saved one, or the engine's when "default").</summary>
        public static int EffectiveQualityLevel =>
            Current.QualityLevel >= 0 ? Current.QualityLevel : QualitySettings.GetQualityLevel();

        public static bool ShowDamageNumbers => Current.ShowDamageNumbers;

        /// <summary>Writes any pending (debounced) change now — call when the Settings screen closes.</summary>
        public static void Flush()
        {
            if (Instance != null && Instance._saveAt >= 0f)
            {
                Instance._saveAt = -1f;
                SaveService.MarkDirty();
            }
        }

        // ------------------------------------------------------------------ haptics

        /// <summary>
        /// Short vibration on Android/iOS, respecting the Vibration toggle. No-op on other
        /// platforms (Handheld only exists on mobile). <paramref name="force"/> ignores the toggle.
        /// </summary>
        public static void Vibrate(bool force = false)
        {
            if (!force && !Current.Vibration) return;
#if UNITY_ANDROID || UNITY_IOS
            if (Application.isMobilePlatform) Handheld.Vibrate();
#endif
        }

        // ------------------------------------------------------------------ apply

        /// <summary>Sanitizes and applies every setting, then broadcasts <see cref="SettingsChangedEvent"/>.</summary>
        public static void ApplyAll()
        {
            var settings = Current;
            settings.Sanitize(QualitySettings.names.Length);
            ApplyQuality(settings);
            ApplyFrameRate(settings);
            NinjaVillage.Gameplay.Camera.CameraShake.Enabled = settings.ScreenShake;
            EventBus<SettingsChangedEvent>.Raise(new SettingsChangedEvent(settings));
        }

        private static void ApplyQuality(SettingsSaveData settings)
        {
            if (settings.QualityLevel < 0 || settings.QualityLevel >= QualitySettings.names.Length) return;
            if (QualitySettings.GetQualityLevel() != settings.QualityLevel)
                QualitySettings.SetQualityLevel(settings.QualityLevel, true);
        }

        private static void ApplyFrameRate(SettingsSaveData settings)
        {
            // vSync would override targetFrameRate on desktop/editor; mobile ignores vSyncCount.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = SettingsSaveData.NormalizeFrameRate(settings.TargetFrameRate);
        }

        private static void SetVolume(ref float field, float value)
        {
            float clamped = SettingsSaveData.ClampVolume(value);
            if (Mathf.Approximately(field, clamped)) return;
            field = clamped;
            Commit(immediate: false);
        }

        private static void Commit(bool immediate)
        {
            EventBus<SettingsChangedEvent>.Raise(new SettingsChangedEvent(Current));

            if (immediate || Instance == null)
            {
                SaveService.MarkDirty();
                if (Instance != null) Instance._saveAt = -1f;
            }
            else
            {
                Instance._saveAt = Time.unscaledTime + SaveDebounceSeconds;
            }
        }

        // ------------------------------------------------------------------ lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable() => EventBus<SaveLoadedEvent>.SubscribePersistent(OnSaveLoaded);

        private void OnDisable() => EventBus<SaveLoadedEvent>.UnsubscribePersistent(OnSaveLoaded);

        private void Start() => ApplyAll();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_saveAt >= 0f && Time.unscaledTime >= _saveAt)
            {
                _saveAt = -1f;
                SaveService.MarkDirty();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Flush();
        }

        private void OnSaveLoaded(SaveLoadedEvent evt) => ApplyAll();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[SettingsService]");
            go.hideFlags = HideFlags.HideInHierarchy;
            go.AddComponent<SettingsService>();
        }
    }
}
