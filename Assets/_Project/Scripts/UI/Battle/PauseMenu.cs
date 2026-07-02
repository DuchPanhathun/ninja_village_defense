using UnityEngine;

namespace NinjaVillage.UI.Battle
{
    /// <summary>
    /// Minimal pause menu. Wire the HUD pause button to <see cref="TogglePause"/>
    /// and the panel's Resume button to it as well.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;

        public bool IsPaused { get; private set; }

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
            pausePanel.SetActive(true);
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            pausePanel.SetActive(false);
        }
    }
}
