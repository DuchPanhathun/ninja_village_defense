using NinjaVillage.Core.Combat;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Bosses;
using UnityEngine;

namespace NinjaVillage.Gameplay.Camera
{
    /// <summary>Smoothly zooms the camera out while a boss is alive, and back in once it dies.</summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class BossZoomEffect : MonoBehaviour
    {
        [SerializeField] private float zoomedOutSize = 9f;
        [SerializeField] private float zoomSpeed = 3f;

        private UnityEngine.Camera _camera;
        private float _defaultSize;
        private float _targetSize;
        private Health _trackedBossHealth;

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            _defaultSize = _camera.orthographicSize;
            _targetSize = _defaultSize;
        }

        private void OnEnable() => EventBus<BossSpawnedEvent>.Subscribe(OnBossSpawned);

        private void OnDisable()
        {
            EventBus<BossSpawnedEvent>.Unsubscribe(OnBossSpawned);
            if (_trackedBossHealth != null)
                _trackedBossHealth.OnDeath -= OnBossDeath;
        }

        private void OnBossSpawned(BossSpawnedEvent evt)
        {
            _targetSize = zoomedOutSize;
            _trackedBossHealth = evt.Health;
            _trackedBossHealth.OnDeath += OnBossDeath;
        }

        private void OnBossDeath(Health health)
        {
            _targetSize = _defaultSize;
            health.OnDeath -= OnBossDeath;
            _trackedBossHealth = null;
        }

        private void Update()
        {
            if (Mathf.Approximately(_camera.orthographicSize, _targetSize)) return;
            _camera.orthographicSize = Mathf.MoveTowards(_camera.orthographicSize, _targetSize, zoomSpeed * Time.deltaTime);
        }
    }
}
