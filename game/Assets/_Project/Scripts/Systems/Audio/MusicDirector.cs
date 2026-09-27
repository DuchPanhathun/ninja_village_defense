using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Bosses;
using NinjaVillage.Systems.GameFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NinjaVillage.Systems.Audio
{
    /// <summary>
    /// Picks the music track from game context so no scene or gameplay script has to (EPIC 18
    /// "Background music", "Battle music", "Boss music"): menu / village / battle music by
    /// scene, boss music while a boss is alive, and a victory/defeat stinger when a run ends.
    /// Lives on the AudioManager's GameObject and listens with SubscribePersistent.
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        [SerializeField] private float sceneFadeSeconds = 1.2f;
        [SerializeField] private float bossFadeSeconds = 0.6f;

        private int _bossesAlive;

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            EventBus<BossSpawnedEvent>.SubscribePersistent(OnBossSpawned);
            EventBus<BossDefeatedEvent>.SubscribePersistent(OnBossDefeated);
            EventBus<RunEndedEvent>.SubscribePersistent(OnRunEnded);
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            EventBus<BossSpawnedEvent>.UnsubscribePersistent(OnBossSpawned);
            EventBus<BossDefeatedEvent>.UnsubscribePersistent(OnBossDefeated);
            EventBus<RunEndedEvent>.UnsubscribePersistent(OnRunEnded);
        }

        private void Start() => PlayForScene(SceneManager.GetActiveScene().name);

        private void OnActiveSceneChanged(Scene previous, Scene next)
        {
            _bossesAlive = 0;
            PlayForScene(next.name);
        }

        /// <summary>Scene name → default track. Unknown scenes (test scenes) keep whatever is playing.</summary>
        public static string TrackForScene(string sceneName)
        {
            switch (sceneName)
            {
                case SceneNames.MainMenu: return AudioCueIds.MusicMenu;
                case SceneNames.Village: return AudioCueIds.MusicVillage;
                case SceneNames.Battle: return AudioCueIds.MusicBattle;
                default: return null;
            }
        }

        private void PlayForScene(string sceneName)
        {
            string track = TrackForScene(sceneName);
            if (track != null) Sfx.Music(track, sceneFadeSeconds);
        }

        private void OnBossSpawned(BossSpawnedEvent evt)
        {
            _bossesAlive++;
            Sfx.Music(AudioCueIds.MusicBoss, bossFadeSeconds);
        }

        private void OnBossDefeated(BossDefeatedEvent evt)
        {
            _bossesAlive = Mathf.Max(0, _bossesAlive - 1);
            if (_bossesAlive == 0) Sfx.Music(AudioCueIds.MusicBattle, sceneFadeSeconds);
        }

        private void OnRunEnded(RunEndedEvent evt)
        {
            _bossesAlive = 0;
            Sfx.Music(evt.Victory ? AudioCueIds.MusicVictory : AudioCueIds.MusicDefeat, bossFadeSeconds);
        }
    }
}
