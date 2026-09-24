using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Battle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NinjaVillage.Systems.Performance
{
    /// <summary>
    /// Mobile and battery defaults (EPIC 23 "Mobile optimization", "Battery optimization"):
    /// <list type="bullet">
    /// <item>Screen stays awake during battles (the player may hold still), but uses the system sleep
    /// timeout in menus and the village so an idle phone can dim and sleep.</item>
    /// <item>Leaving the app mid-battle pauses the run, so nothing simulates (or kills the player) in the
    /// background and the CPU can idle.</item>
    /// <item>Caps a single frame's delta time so a slow frame on a low-end device can't cause a spiral of
    /// catch-up physics steps.</item>
    /// </list>
    /// The frame-rate cap (30/60 battery saver) is owned by the Settings service.
    /// </summary>
    public class PerformanceBootstrap : MonoBehaviour
    {
        public const float MaxFrameDeltaSeconds = 0.1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Time.maximumDeltaTime = MaxFrameDeltaSeconds;
            var go = new GameObject("[PerformanceBootstrap]");
            DontDestroyOnLoad(go);
            go.AddComponent<PerformanceBootstrap>();
        }

        private void OnEnable() => SceneManager.activeSceneChanged += OnSceneChanged;
        private void OnDisable() => SceneManager.activeSceneChanged -= OnSceneChanged;

        private void Start() => ApplySleepPolicy(SceneManager.GetActiveScene().name);

        private void OnSceneChanged(Scene previous, Scene next) => ApplySleepPolicy(next.name);

        public static int SleepTimeoutFor(string sceneName) =>
            sceneName == SceneNames.Battle ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;

        private static void ApplySleepPolicy(string sceneName) => Screen.sleepTimeout = SleepTimeoutFor(sceneName);

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            var pauseMenu = FindAnyObjectByType<PauseMenu>();
            if (pauseMenu != null && !pauseMenu.IsPaused && Time.timeScale > 0f) pauseMenu.Pause();
        }
    }
}
