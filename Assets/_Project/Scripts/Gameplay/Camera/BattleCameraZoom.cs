using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Settings;
using UnityEngine;

namespace NinjaVillage.Gameplay.Camera
{
    /// <summary>
    /// The player's battle view — Normal, Wide or Widest (<see cref="SettingsSaveData.BattleZoom"/>): scales the battle
    /// camera's size from the scene's own, easing there when the setting changes (instantly at the start of a run).
    /// Added to the battle camera when the Battle scene loads. Spawns keep off-screen at any zoom
    /// (<see cref="Waves.SpawnManager.OffScreenDistance"/>).
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class BattleCameraZoom : MonoBehaviour
    {
        /// <summary>Camera size change per second, as a share of the normal size.</summary>
        private const float EaseSpeed = 2.5f;

        private UnityEngine.Camera _camera;
        private float _baseSize;

        /// <summary>The scene's own camera size (the Normal view).</summary>
        public float BaseSize => _baseSize;
        /// <summary>The size the camera is heading to for the saved level.</summary>
        public float TargetSize => _baseSize * SettingsSaveData.BattleZoomScale(SettingsService.Current.BattleZoom);

        /// <summary>Adds the zoom to <paramref name="camera"/> (once) and jumps straight to the saved view.</summary>
        public static BattleCameraZoom Ensure(UnityEngine.Camera camera)
        {
            if (camera == null || !camera.orthographic) return null;
            var zoom = camera.GetComponent<BattleCameraZoom>();
            if (zoom == null) zoom = camera.gameObject.AddComponent<BattleCameraZoom>();
            zoom.Snap();
            return zoom;
        }

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            _baseSize = _camera.orthographicSize;
        }

        public void Snap() => _camera.orthographicSize = TargetSize;

        private void LateUpdate()
        {
            float target = TargetSize;
            if (Mathf.Approximately(_camera.orthographicSize, target)) return;
            // Unscaled: a change made from the pause menu is already in place when play resumes.
            _camera.orthographicSize = Mathf.MoveTowards(_camera.orthographicSize, target, _baseSize * EaseSpeed * Time.unscaledDeltaTime);
        }
    }
}
