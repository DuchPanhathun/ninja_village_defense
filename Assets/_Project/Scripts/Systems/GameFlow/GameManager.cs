using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Waves;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NinjaVillage.Systems.GameFlow
{
    /// <summary>
    /// Battle-scene run orchestrator: loads the save on start, tracks the current
    /// wave, ends the run on player death or full clear, persists progress, and
    /// restarts cleanly.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private SaveData _saveData;
        private int _currentWave;
        private bool _runEnded;

        private void Awake()
        {
            _saveData = SaveSystem.Load();
        }

        private void Start()
        {
            // EconomyManager may be created in the same frame; wallet sync in Start is safe.
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.LoadFrom(_saveData.Wallet);
        }

        private void OnEnable()
        {
            EventBus<WaveStartedEvent>.Subscribe(OnWaveStarted);
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
            EventBus<AllWavesCompleteEvent>.Subscribe(OnAllWavesComplete);
        }

        private void OnDisable()
        {
            EventBus<WaveStartedEvent>.Unsubscribe(OnWaveStarted);
            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);
            EventBus<AllWavesCompleteEvent>.Unsubscribe(OnAllWavesComplete);
        }

        private void OnWaveStarted(WaveStartedEvent evt) => _currentWave = evt.WaveNumber;

        private void OnPlayerDied(PlayerDiedEvent evt) => EndRun(victory: false);

        private void OnAllWavesComplete(AllWavesCompleteEvent evt) => EndRun(victory: true);

        private void EndRun(bool victory)
        {
            if (_runEnded) return;
            _runEnded = true;

            _saveData.HighestWaveReached = Mathf.Max(_saveData.HighestWaveReached, _currentWave);
            _saveData.TotalRunsCompleted++;

            if (EconomyManager.Instance != null)
            {
                _saveData.Wallet.Coins = EconomyManager.Instance.Wallet.Coins;
                _saveData.Wallet.Gems = EconomyManager.Instance.Wallet.Gems;
            }

            SaveSystem.Save(_saveData);
            EventBus<RunEndedEvent>.Raise(new RunEndedEvent(victory, _currentWave));
        }

        /// <summary>Hook the game-over panel's Retry button here.</summary>
        public void RestartRun()
        {
            Time.timeScale = 1f;
            // Static event channels survive scene loads; clear them so handlers on
            // destroyed objects don't linger into the next run.
            EventBusRegistry.ClearAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
