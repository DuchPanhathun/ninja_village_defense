using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using UnityEngine;

namespace NinjaVillage.UI.Battle
{
    /// <summary>
    /// Pause panel. Besides the scene's Resume button it adds "Quit to Village" / "Quit to Home" (built
    /// in code the first time it opens); quitting records the run as a defeat via
    /// <see cref="GameManager.LeaveTo"/>, so wave progress and coins still count.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;

        public bool IsPaused { get; private set; }

        private bool _extrasBuilt;

        private void Awake()
        {
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        public void TogglePause()
        {
            if (IsPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            if (pausePanel == null) return;
            pausePanel.SetActive(true);
            BuildExtras();
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        private void BuildExtras()
        {
            if (_extrasBuilt) return;
            _extrasBuilt = true;

            var column = UIBuilder.Vertical(pausePanel.transform, "QuitButtons", 16f, 0);
            var rt = (RectTransform)column.transform;
            rt.anchorMin = new Vector2(0.15f, 0.1f);
            rt.anchorMax = new Vector2(0.85f, 0.26f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var gameManager = FindAnyObjectByType<GameManager>();
            UIBuilder.Button(column.transform, "Quit to Village", () => Leave(gameManager, SceneNames.Village), UITheme.ButtonSecondary, 100f);
            UIBuilder.Button(column.transform, "Quit to Home", () => Leave(gameManager, SceneNames.MainMenu), UITheme.ButtonSecondary, 100f);
        }

        private static void Leave(GameManager gameManager, string scene)
        {
            if (gameManager != null) gameManager.LeaveTo(scene);
            else SceneLoader.Load(scene);
        }
    }
}
