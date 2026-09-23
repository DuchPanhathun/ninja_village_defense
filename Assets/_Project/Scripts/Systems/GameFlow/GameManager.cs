using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Waves;
using NinjaVillage.Systems.Meta;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.GameFlow
{
    /// <summary>
    /// Battle-scene run orchestrator: tracks the current wave, ends the run on player
    /// death or full clear, records the run into the profile, persists progress, and
    /// restarts / exits cleanly.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Tooltip("Optional — supplies kills/coins/duration for the run record. Found on this GameObject if left empty.")]
        [SerializeField] private RunStatsTracker runStats;

        private int _currentWave;
        private bool _runEnded;

        private void Awake()
        {
            if (runStats == null) runStats = GetComponent<RunStatsTracker>();
            if (runStats == null) runStats = gameObject.AddComponent<RunStatsTracker>();
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

            var save = SaveService.Data;
            var record = runStats.BuildRecord(victory, _currentWave);

            save.Profile.RecordRun(record);
            // Legacy root fields, still read by older UI.
            save.HighestWaveReached = Mathf.Max(save.HighestWaveReached, _currentWave);
            save.TotalRunsCompleted++;

            SaveService.SaveNow();
            EventBus<RunEndedEvent>.Raise(new RunEndedEvent(victory, _currentWave, record));
        }

        /// <summary>Hook the game-over panel's Retry button here.</summary>
        public void RestartRun() => SceneLoader.ReloadActive();

        /// <summary>Hook the game-over / pause panel's "Village" button here.</summary>
        public void ReturnToVillage() => SceneLoader.LoadVillage();

        /// <summary>Hook the game-over / pause panel's "Home" button here.</summary>
        public void ReturnToMainMenu() => SceneLoader.LoadMainMenu();
    }
}
