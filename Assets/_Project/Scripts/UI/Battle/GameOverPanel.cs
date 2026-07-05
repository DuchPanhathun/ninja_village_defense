using NinjaVillage.Core.Events;
using NinjaVillage.Systems.GameFlow;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Battle
{
    /// <summary>Run-end screen: victory/defeat headline + wave reached. Wire Retry to GameManager.RestartRun.</summary>
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text waveText;

        private void Awake()
        {
            // Subscribe before disabling panelRoot below — if this script lives on
            // panelRoot itself, OnEnable would never fire once it's inactive.
            EventBus<RunEndedEvent>.Subscribe(OnRunEnded);

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void OnDestroy() => EventBus<RunEndedEvent>.Unsubscribe(OnRunEnded);

        private void OnRunEnded(RunEndedEvent evt)
        {
            panelRoot.SetActive(true);
            Time.timeScale = 0f;

            if (titleText != null)
                titleText.text = evt.Victory ? "VICTORY!" : "DEFEATED";
            if (waveText != null)
                waveText.text = $"Wave {evt.WaveReached}";
        }
    }
}
