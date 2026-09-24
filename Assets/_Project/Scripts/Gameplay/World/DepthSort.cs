using UnityEngine;

namespace NinjaVillage.Gameplay.World
{
    /// <summary>
    /// Top-down depth for battle: characters and solid scenery draw in order of where they touch the
    /// ground — lower on screen is in front — so you walk behind a tree when you're above it and in front
    /// of it when you're below. Orders are relative to the camera (the world is endless) and live in a band
    /// under pickups, projectiles and VFX. Airborne characters (jumps, boss leaps) draw above everything in
    /// the band. Child renderers keep their original order as an offset (shadows stay under bodies).
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class DepthSort : MonoBehaviour
    {
        public const int Band = -600;
        public const int AirborneBonus = 400;
        private const float PerUnit = 10f;
        private const int Range = 250;

        private SpriteRenderer[] _renderers = System.Array.Empty<SpriteRenderer>();
        private int[] _offsets = System.Array.Empty<int>();
        private float _footOffset;

        /// <summary>Set while jumping/leaping.</summary>
        public bool Airborne { get; set; }

        public static int OrderFor(float feetY)
        {
            var cam = UnityEngine.Camera.main;
            float camY = cam != null ? cam.transform.position.y : 0f;
            return Band + Mathf.Clamp(Mathf.RoundToInt(-(feetY - camY) * PerUnit), -Range, Range);
        }

        private void Start() => Collect();

        /// <summary>Re-reads the child renderers (call after adding/removing sprite children).</summary>
        public void Collect()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            _offsets = new int[_renderers.Length];
            int baseOrder = int.MaxValue;
            SpriteRenderer main = null;
            foreach (var r in _renderers)
                if (r.sortingOrder < baseOrder) baseOrder = r.sortingOrder;
            for (int i = 0; i < _renderers.Length; i++)
            {
                _offsets[i] = Mathf.Clamp(_renderers[i].sortingOrder - baseOrder, 0, 50);
                if (main == null && _renderers[i].sprite != null) main = _renderers[i];
            }
            _footOffset = main != null ? main.bounds.min.y - transform.position.y : 0f;
        }

        private void LateUpdate()
        {
            int order = OrderFor(transform.position.y + _footOffset) + (Airborne ? AirborneBonus : 0);
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].sortingOrder = order + _offsets[i];
        }
    }
}
