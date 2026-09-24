using UnityEngine;

namespace NinjaVillage.Gameplay.World
{
    /// <summary>
    /// Endless tiled ground for the battle: a tiled SpriteRenderer a little larger than the camera view
    /// that re-centres on the camera in whole-tile steps, so the pattern never visibly slides.
    /// Follows camera zoom too (boss zoom-out). The sprite needs Full Rect mesh type for tiling.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class InfiniteGround : MonoBehaviour
    {
        [Tooltip("Main camera if empty.")]
        [SerializeField] private UnityEngine.Camera targetCamera;
        [SerializeField] private float margin = 2f;

        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.drawMode = SpriteDrawMode.Tiled;
        }

        private void LateUpdate()
        {
            var cam = targetCamera != null ? targetCamera : UnityEngine.Camera.main;
            if (cam == null || _renderer.sprite == null) return;

            Vector2 tile = _renderer.sprite.bounds.size;
            if (tile.x <= 0f || tile.y <= 0f) return;

            float height = cam.orthographicSize * 2f + margin * 2f;
            float width = cam.orthographicSize * 2f * cam.aspect + margin * 2f;
            var size = new Vector2((Mathf.Ceil(width / tile.x) + 1f) * tile.x, (Mathf.Ceil(height / tile.y) + 1f) * tile.y);
            if (_renderer.size != size) _renderer.size = size;

            Vector3 c = cam.transform.position;
            transform.position = new Vector3(Mathf.Round(c.x / tile.x) * tile.x, Mathf.Round(c.y / tile.y) * tile.y, transform.position.z);
        }
    }
}
