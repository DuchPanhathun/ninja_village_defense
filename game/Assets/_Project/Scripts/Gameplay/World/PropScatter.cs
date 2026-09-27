using System.Collections.Generic;
using UnityEngine;

namespace NinjaVillage.Gameplay.World
{
    /// <summary>
    /// Decorates the endless ground with a chapter's props (trees, rocks, bushes...). The world is split
    /// into cells; whether a cell holds a prop, which one and where is a pure hash of the cell, so the
    /// same spot always shows the same prop when the player comes back. Only cells near the camera exist
    /// (pooled renderers). Trees, rocks, stumps and statues are solid: a collider on their base (the
    /// <see cref="Obstacles"/> layer — the player and enemies must go around or jump over) and depth-sorted
    /// with the characters so you can walk behind a canopy. Bushes and flowers are flat ground decoration.
    /// </summary>
    public class PropScatter : MonoBehaviour
    {
        [SerializeField] private float cellSize = 3.2f;
        [SerializeField] private float margin = 3f;
        [SerializeField] private int seed = 1234;
        [Tooltip("Flat props (bushes, flowers) sort between the ground (-1000) and everything else.")]
        [SerializeField] private int baseSortingOrder = -900;
        [Tooltip("Largest side of a prop in world units; the pack's 64 px boulders and big trees would fill half the view.")]
        [SerializeField] private float maxPropSize = 2.6f;

        private Sprite[] _sprites = System.Array.Empty<Sprite>();
        private float _density = 0.35f;
        private Color _tint = Color.white;
        private readonly Dictionary<Vector2Int, SpriteRenderer> _active = new();
        private readonly Stack<SpriteRenderer> _pool = new();
        private readonly List<Vector2Int> _toRemove = new();
        private readonly HashSet<Vector2Int> _wanted = new();
        private readonly List<SpriteRenderer> _solid = new();

        /// <summary>Flat ground cover you can walk over; everything else stands in the way.</summary>
        public static bool IsSolid(Sprite sprite) =>
            sprite != null && !sprite.name.Contains("bush") && !sprite.name.Contains("flower") && !sprite.name.Contains("grass");

        public void Configure(IReadOnlyList<Sprite> sprites, float density, Color tint, int newSeed)
        {
            var list = new List<Sprite>();
            if (sprites != null)
                foreach (var sprite in sprites)
                    if (sprite != null) list.Add(sprite);
            _sprites = list.ToArray();
            _density = Mathf.Clamp01(density);
            _tint = tint;
            seed = newSeed;
            foreach (var renderer in _active.Values) Release(renderer);
            _active.Clear();
        }

        private void LateUpdate()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null || _sprites.Length == 0 || _density <= 0f) return;

            float halfH = cam.orthographicSize + margin, halfW = cam.orthographicSize * cam.aspect + margin;
            Vector2 c = cam.transform.position;
            var min = new Vector2Int(Mathf.FloorToInt((c.x - halfW) / cellSize), Mathf.FloorToInt((c.y - halfH) / cellSize));
            var max = new Vector2Int(Mathf.FloorToInt((c.x + halfW) / cellSize), Mathf.FloorToInt((c.y + halfH) / cellSize));

            _wanted.Clear();
            for (int y = min.y; y <= max.y; y++)
                for (int x = min.x; x <= max.x; x++)
                    _wanted.Add(new Vector2Int(x, y));

            // Solid props sort with the characters; the camera moves, so every frame.
            foreach (var solid in _solid)
                solid.sortingOrder = DepthSort.OrderFor(solid.bounds.min.y);

            _toRemove.Clear();
            foreach (var cell in _active.Keys)
                if (!_wanted.Contains(cell)) _toRemove.Add(cell);
            foreach (var cell in _toRemove)
            {
                Release(_active[cell]);
                _active.Remove(cell);
            }

            foreach (var cell in _wanted)
            {
                if (_active.ContainsKey(cell)) continue;
                _active[cell] = Place(cell); // null (no prop) is remembered too, so the hash runs once per visit
            }
        }

        private SpriteRenderer Place(Vector2Int cell)
        {
            uint h = Hash(cell.x, cell.y, seed);
            if ((h & 0xFFFF) / 65535f >= _density) return null;
            // Keep the spawn point clear so the run doesn't start inside a tree.
            if (Mathf.Abs(cell.x) <= 1 && Mathf.Abs(cell.y) <= 1) return null;

            var sprite = _sprites[(int)((h >> 16) % (uint)_sprites.Length)];
            uint h2 = Hash(cell.y, cell.x, seed ^ 0x5bd1e995);
            var offset = new Vector2(((h2 & 0xFF) / 255f - 0.5f) * cellSize * 0.7f, (((h2 >> 8) & 0xFF) / 255f - 0.5f) * cellSize * 0.7f);
            var position = (Vector2)cell * cellSize + Vector2.one * (cellSize * 0.5f) + offset;

            var renderer = _pool.Count > 0 ? _pool.Pop() : CreateRenderer();
            renderer.gameObject.SetActive(true);
            renderer.sprite = sprite;
            renderer.color = _tint;
            renderer.flipX = ((h2 >> 16) & 1) == 1;
            renderer.transform.position = position;
            Vector2 native = sprite.bounds.size;
            float largest = Mathf.Max(native.x, native.y);
            renderer.transform.localScale = Vector3.one * (largest > maxPropSize ? maxPropSize / largest : 1f);

            // Solid: a box over the base (trunk / bottom of the rock), in the sprite's own units.
            bool solid = IsSolid(sprite) && Obstacles.Available;
            var box = renderer.GetComponent<BoxCollider2D>();
            box.enabled = solid;
            renderer.gameObject.layer = solid ? Obstacles.Layer : 0;
            if (solid)
            {
                box.size = new Vector2(native.x * 0.6f, native.y * 0.34f);
                box.offset = new Vector2(0f, -native.y * 0.5f + native.y * 0.17f);
                _solid.Add(renderer);
            }
            // Flat props lie on the ground under everything; solid ones are depth-sorted each frame.
            renderer.sortingOrder = solid ? DepthSort.OrderFor(renderer.bounds.min.y) : baseSortingOrder;
            return renderer;
        }

        private SpriteRenderer CreateRenderer()
        {
            var go = new GameObject("Prop");
            go.transform.SetParent(transform, false);
            go.AddComponent<BoxCollider2D>().enabled = false;
            return go.AddComponent<SpriteRenderer>();
        }

        private void Release(SpriteRenderer renderer)
        {
            if (renderer == null) return;
            _solid.Remove(renderer);
            renderer.gameObject.SetActive(false);
            _pool.Push(renderer);
        }

        private static uint Hash(int x, int y, int salt)
        {
            unchecked
            {
                uint h = (uint)x * 0x8da6b343u ^ (uint)y * 0xd8163841u ^ (uint)salt * 0xcb1ab31fu;
                h ^= h >> 13;
                h *= 0x5bd1e995u;
                h ^= h >> 15;
                return h;
            }
        }
    }
}
