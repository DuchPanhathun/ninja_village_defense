using UnityEngine;

namespace NinjaVillage.Core.Utilities
{
    /// <summary>
    /// Art-free placeholder shapes (square, circle, ring, roof triangle, soft glow) generated once from
    /// small textures and cached. Lets the village, NPCs and VFX render with plain SpriteRenderers —
    /// which use URP's default sprite material — until real art exists, without relying on
    /// ParticleSystem materials or Shader.Find (shaders can be stripped from builds).
    /// All sprites are white; tint them with SpriteRenderer.color. 1 world unit = 1 sprite width.
    /// </summary>
    public static class GeneratedSprites
    {
        private const int Size = 64;

        private static Sprite _square, _circle, _ring, _triangle, _glow, _arc;

        public static Sprite Square => _square != null ? _square : _square = Make("GenSquare", (x, y) => 1f);

        public static Sprite Circle => _circle != null ? _circle : _circle = Make("GenCircle", (x, y) =>
            Mathf.Clamp01((0.5f - Dist(x, y)) * Size));

        public static Sprite Ring => _ring != null ? _ring : _ring = Make("GenRing", (x, y) =>
        {
            float d = Dist(x, y);
            return Mathf.Clamp01((0.5f - d) * Size) * Mathf.Clamp01((d - 0.38f) * Size);
        });

        /// <summary>Isosceles triangle pointing up (roofs, arrows).</summary>
        public static Sprite Triangle => _triangle != null ? _triangle : _triangle = Make("GenTriangle", (x, y) =>
        {
            float halfWidthAtY = 0.5f * (1f - y);
            return Mathf.Abs(x - 0.5f) <= halfWidthAtY ? 1f : 0f;
        });

        /// <summary>Radial soft falloff — flashes, bursts, glows.</summary>
        public static Sprite Glow => _glow != null ? _glow : _glow = Make("GenGlow", (x, y) =>
        {
            float t = Mathf.Clamp01(1f - Dist(x, y) * 2f);
            return t * t;
        });

        /// <summary>A 120° thick crescent opening toward +X — sword slash arcs. Rotate to aim.</summary>
        public static Sprite Arc => _arc != null ? _arc : _arc = Make("GenArc", (x, y) =>
        {
            float d = Dist(x, y);
            float angle = Mathf.Abs(Mathf.Atan2(y - 0.5f, x - 0.5f) * Mathf.Rad2Deg);
            float band = Mathf.Clamp01((0.5f - d) * Size) * Mathf.Clamp01((d - 0.3f) * Size);
            float taper = Mathf.Clamp01((60f - angle) / 20f);
            return band * taper;
        });

        private static float Dist(float x, float y) => Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f));

        private static Sprite Make(string name, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float a = Mathf.Clamp01(alpha((x + 0.5f) / Size, (y + 0.5f) / Size));
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        /// <summary>Creates a child GameObject with a tinted SpriteRenderer.</summary>
        public static SpriteRenderer CreateRenderer(Transform parent, string name, Sprite sprite, Color color, int sortingOrder,
            Vector2 localPosition = default, Vector2? scale = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            if (scale.HasValue) go.transform.localScale = new Vector3(scale.Value.x, scale.Value.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
