using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NinjaVillage.Systems.GameFlow
{
    /// <summary>Scene names — must match the scene files listed in Build Profiles → Scene List.</summary>
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string Village = "Village";
        public const string Battle = "Battle";
    }

    /// <summary>
    /// The one way to change scenes. Flushes the save, restores time scale (a paused
    /// battle leaves it at 0), and clears scene-scoped event listeners so nothing from
    /// the old scene lingers. Persistent services use EventBus SubscribePersistent and
    /// are unaffected.
    /// </summary>
    public static class SceneLoader
    {
        public static void Load(string sceneName)
        {
            string from = SceneManager.GetActiveScene().name;
            EventBus<SceneChangingEvent>.Raise(new SceneChangingEvent(from, sceneName));

            SaveService.SaveNow();
            Time.timeScale = 1f;
            EventBusRegistry.ClearAll();
            SceneManager.LoadScene(sceneName);
        }

        public static void ReloadActive() => Load(SceneManager.GetActiveScene().name);

        public static void LoadMainMenu() => Load(SceneNames.MainMenu);
        public static void LoadVillage() => Load(SceneNames.Village);
        public static void LoadBattle() => Load(SceneNames.Battle);
    }
}
