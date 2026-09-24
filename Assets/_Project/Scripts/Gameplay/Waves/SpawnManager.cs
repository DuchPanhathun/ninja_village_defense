using NinjaVillage.Gameplay.Enemies;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Gameplay.Waves
{
    /// <summary>
    /// Instantiates enemies and picks spawn positions in a ring around the player,
    /// far enough out to stay off-screen. Kept separate from WaveManager so other
    /// systems (e.g. a future "elite patrol" feature) can reuse it.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField] private float minSpawnRadius = 8f;
        [SerializeField] private float maxSpawnRadius = 11f;
        [Tooltip("Cap on enemies alive at once (EPIC 23 mobile performance). The WaveManager waits for room instead of dropping spawns.")]
        [SerializeField] private int maxAliveEnemies = 150;

        private int _alive;
        public int AliveEnemies => _alive;
        /// <summary>False while the arena is at the enemy cap — spawners should wait, not skip.</summary>
        public bool CanSpawn => _alive < maxAliveEnemies;

        private void OnEnable() => EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
        private void OnDisable() => EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
        private void OnEnemyKilled(EnemyKilledEvent evt) => _alive = Mathf.Max(0, _alive - 1);

        public EnemyController Spawn(EnemyDefinition definition, Vector2 position, float difficultyMultiplier = 1f, bool forceElite = false)
        {
            if (definition.EnemyPrefab == null)
            {
                Debug.LogWarning($"EnemyDefinition '{definition.DisplayName}' has no prefab assigned.", this);
                return null;
            }

            var instance = Instantiate(definition.EnemyPrefab, position, Quaternion.identity);
            _alive++;
            var controller = instance.GetComponent<EnemyController>();
            controller.Initialize(definition, difficultyMultiplier, forceElite);
            return controller;
        }

        public Vector2 GetSpawnPositionAroundPlayer()
        {
            Vector2 playerPos = PlayerReference.Instance != null
                ? (Vector2)PlayerReference.Instance.PlayerTransform.position
                : Vector2.zero;

            Vector2 direction = Random.insideUnitCircle.normalized;
            float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
            return playerPos + direction * radius;
        }
    }
}
