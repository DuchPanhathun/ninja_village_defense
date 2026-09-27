using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Waves;
using NinjaVillage.Systems.Chapters;
using NinjaVillage.Systems.Meta;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.GameFlow
{
    /// <summary>
    /// Battle-scene run orchestrator: tracks the current wave, ends the run on player
    /// death or full clear (a chapter's final boss beaten), records the run into the profile and
    /// the chapter progress, persists progress, and restarts / exits cleanly.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Tooltip("Optional — supplies kills/coins/duration for the run record. Found on this GameObject if left empty.")]
        [SerializeField] private RunStatsTracker runStats;

        [Tooltip("Offer one 'watch an ad to revive' per run before the run is finalized.")]
        [SerializeField] private bool offerRevive = true;
        [SerializeField, Range(0.1f, 1f)] private float reviveHealthFraction = 0.5f;

        private int _currentWave;
        private bool _runEnded;
        private bool _revived;
        private bool _awaitingRevive;

        /// <summary>Whether a revive prompt would be shown if the player died now.</summary>
        public bool CanOfferRevive => offerRevive && !_revived && !_runEnded;

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

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            // Offer the revive BEFORE finalizing, so a revived run is recorded once, at its real end.
            if (CanOfferRevive)
            {
                _awaitingRevive = true;
                Time.timeScale = 0f;
                EventBus<RevivePromptEvent>.Raise(new RevivePromptEvent());
                return;
            }
            EndRun(victory: false);
        }

        public void AcceptRevive()
        {
            if (!_awaitingRevive) return;
            _awaitingRevive = false;
            _revived = true;
            Time.timeScale = 1f;
            if (PlayerReference.Instance != null && PlayerReference.Instance.TryGetComponent<PlayerController>(out var player))
            {
                LayerMask enemies = PlayerReference.Instance.TryGetComponent<AutoAttackController>(out var attack) ? attack.EnemyMask : default;
                player.Revive(reviveHealthFraction, enemies);
            }
        }

        public void DeclineRevive()
        {
            if (!_awaitingRevive) return;
            _awaitingRevive = false;
            EndRun(victory: false);
        }

        private void OnAllWavesComplete(AllWavesCompleteEvent evt) => EndRun(victory: true);

        private void EndRun(bool victory)
        {
            if (_runEnded) return;
            _runEnded = true;

            var save = SaveService.Data;
            var record = runStats.BuildRecord(victory, _currentWave);
            var chapter = ChapterDirector.Current;
            if (chapter != null) record.ChapterId = chapter.Id;
            ChapterService.RecordRun(chapter, victory, _currentWave); // best wave, first-clear reward, next chapter

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
        public void ReturnToVillage() => LeaveTo(SceneNames.Village);

        /// <summary>Hook the game-over / pause panel's "Home" button here.</summary>
        public void ReturnToMainMenu() => LeaveTo(SceneNames.MainMenu);

        /// <summary>
        /// Leaves the battle. Quitting mid-run (from the pause menu) records it as a defeat first, so the
        /// wave reached, kills and coins earned still count and quitting can't be used to dodge a loss.
        /// </summary>
        public void LeaveTo(string sceneName)
        {
            if (!_runEnded) EndRun(victory: false);
            SceneLoader.Load(sceneName);
        }
    }
}
