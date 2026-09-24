using System.Collections;
using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Gameplay.Waves
{
    /// <summary>
    /// Sequences the battle's waves: spawns each wave's enemies, waits for the
    /// arena to clear, then advances. After the last defined wave, either stops
    /// (raising <see cref="AllWavesCompleteEvent"/>) or loops the final wave with
    /// rising difficulty if <see cref="endlessMode"/> is enabled.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [SerializeField] private WaveDefinition[] waves;
        [SerializeField] private SpawnManager spawnManager;
        [SerializeField] private float delayBetweenWaves = 3f;
        [SerializeField] private bool endlessMode = true;
        [SerializeField] private float difficultyRampPerWave = 0.15f;
        [SerializeField] private bool autoStart = true;

        private int _currentWaveIndex;
        private int _aliveCount;
        private bool _finishedSpawningCurrentWave;
        private bool _waveInProgress;

        private void OnEnable()
        {
            EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
        }

        private void OnDisable()
        {
            EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
        }

        private void Start()
        {
            if (autoStart) StartCoroutine(RunWaves());
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (!_waveInProgress) return;
            _aliveCount = Mathf.Max(0, _aliveCount - 1);
            TryCompleteWave();
        }

        private IEnumerator RunWaves()
        {
            while (_currentWaveIndex < waves.Length)
            {
                yield return StartCoroutine(RunWave(waves[_currentWaveIndex], _currentWaveIndex));
                _currentWaveIndex++;
                if (_currentWaveIndex < waves.Length)
                    yield return new WaitForSeconds(delayBetweenWaves);
            }

            if (!endlessMode)
            {
                EventBus<AllWavesCompleteEvent>.Raise(new AllWavesCompleteEvent());
                yield break;
            }

            // Endless mode: keep looping the final wave with escalating difficulty.
            var finalWave = waves[waves.Length - 1];
            int loopIndex = waves.Length;
            while (true)
            {
                yield return StartCoroutine(RunWave(finalWave, loopIndex));
                loopIndex++;
                yield return new WaitForSeconds(delayBetweenWaves);
            }
        }

        private IEnumerator RunWave(WaveDefinition wave, int waveNumber)
        {
            _waveInProgress = true;
            _finishedSpawningCurrentWave = false;
            _aliveCount = 0;

            EventBus<WaveStartedEvent>.Raise(new WaveStartedEvent(waveNumber + 1, wave.IsBossWave));
            float difficultyMultiplier = 1f + difficultyRampPerWave * waveNumber;

            foreach (var entry in wave.Spawns)
            {
                for (int i = 0; i < entry.Count; i++)
                {
                    // Wait for room under the live-enemy cap instead of dropping the spawn.
                    while (!spawnManager.CanSpawn) yield return null;
                    if (spawnManager.Spawn(entry.EnemyDefinition, spawnManager.GetSpawnPositionAroundPlayer(), difficultyMultiplier) != null)
                        _aliveCount++; // only count what actually spawned, or the wave could never clear
                    yield return new WaitForSeconds(wave.SpawnInterval);
                }
            }

            if (wave.IsBossWave && wave.BossDefinition != null)
            {
                if (spawnManager.Spawn(wave.BossDefinition, spawnManager.GetSpawnPositionAroundPlayer(), difficultyMultiplier) != null)
                    _aliveCount++;
            }

            _finishedSpawningCurrentWave = true;
            TryCompleteWave();

            // Wait until every spawned enemy in this wave is dead.
            while (_waveInProgress)
                yield return null;

            EventBus<WaveClearedEvent>.Raise(new WaveClearedEvent(waveNumber + 1));
        }

        private void TryCompleteWave()
        {
            if (_finishedSpawningCurrentWave && _aliveCount <= 0)
                _waveInProgress = false;
        }
    }
}
