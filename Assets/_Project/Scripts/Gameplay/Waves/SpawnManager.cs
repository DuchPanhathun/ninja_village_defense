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
        private UnityEngine.Camera _viewCamera;

        /// <summary>The camera whose view spawns stay out of (the main camera unless set).</summary>
        public UnityEngine.Camera ViewCamera
        {
            get => _viewCamera != null ? _viewCamera : UnityEngine.Camera.main;
            set => _viewCamera = value;
        }
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
            if (direction == Vector2.zero) direction = Vector2.up;
            // Past the edge of the view whatever the battle zoom, so nothing pops in on screen.
            float radius = Mathf.Max(Random.Range(minSpawnRadius, maxSpawnRadius), OffScreenDistance(direction, ViewCamera));
            return playerPos + direction * radius;
        }

        /// <summary>
        /// How far from the view's centre, along <paramref name="direction"/> (normalised), the edge of the camera's view is,
        /// plus <paramref name="margin"/>: spawning at least this far keeps enemies off-screen. Uses the size the camera is
        /// zooming to when that is wider. 0 without an orthographic camera (<paramref name="cam"/>, else the main one).
        /// </summary>
        public static float OffScreenDistance(Vector2 direction, UnityEngine.Camera cam = null, float margin = 1.5f)
        {
            if (cam == null) cam = UnityEngine.Camera.main;
            if (cam == null || !cam.orthographic) return 0f;
            var zoom = cam.GetComponent<Camera.BattleCameraZoom>();
            float halfH = Mathf.Max(cam.orthographicSize, zoom != null ? zoom.TargetSize : 0f);
            float halfW = halfH * cam.aspect;
            float tx = Mathf.Abs(direction.x) > 1e-4f ? halfW / Mathf.Abs(direction.x) : float.MaxValue;
            float ty = Mathf.Abs(direction.y) > 1e-4f ? halfH / Mathf.Abs(direction.y) : float.MaxValue;
            return Mathf.Min(tx, ty) + margin;
        }
    }
}
