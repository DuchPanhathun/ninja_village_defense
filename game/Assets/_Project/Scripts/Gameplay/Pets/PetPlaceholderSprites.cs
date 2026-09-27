using UnityEngine;

namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// Generates simple white sprites at runtime (tinted per pet via SpriteRenderer.color) so pets,
    /// bananas, fire breath and treasure markers are visible before any art exists. Textures are
    /// built once and cached; 1 sprite = 1 world unit.
    /// </summary>
    public static class PetPlaceholderSprites
    {
        private const int Size = 64;

        private static Sprite _circle;
        private static Sprite _ring;
        private static Sprite _wedge;

        /// <summary>Soft-edged filled disc.</summary>
        public static Sprite Circle
        {
            get
            {
                if (_circle == null) _circle = Build("PetPlaceholder_Circle", CircleAlpha, new Vector2(0.5f, 0.5f));
                return _circle;
            }
        }

        /// <summary>Hollow ring (treasure markers).</summary>
        public static Sprite Ring
        {
            get
            {
                if (_ring == null) _ring = Build("PetPlaceholder_Ring", RingAlpha, new Vector2(0.5f, 0.5f));
                return _ring;
            }
        }

        /// <summary>60° cone pointing along +X with its pivot at the tip (fire breath).</summary>
        public static Sprite Wedge
        {
            get
            {
                if (_wedge == null) _wedge = Build("PetPlaceholder_Wedge", WedgeAlpha, new Vector2(0f, 0.5f));
                return _wedge;
            }
        }

        private delegate float AlphaFunc(float x, float y);

        private static Sprite Build(string name, AlphaFunc alpha, Vector2 pivot)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // Pixel centre in 0..1 space.
                    float u = (x + 0.5f) / Size;
                    float v = (y + 0.5f) / Size;
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(u, v)) * 255f);
                    pixels[y * Size + x] = new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), pivot, Size);
            sprite.name = name;
            return sprite;
        }

        private static float CircleAlpha(float u, float v)
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f; // 0 centre, 1 edge
            return 1f - Mathf.SmoothStep(0.85f, 1f, d);
        }

        private static float RingAlpha(float u, float v)
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            float outer = 1f - Mathf.SmoothStep(0.88f, 1f, d);
            float inner = Mathf.SmoothStep(0.62f, 0.74f, d);
            return outer * inner;
        }

        private static float WedgeAlpha(float u, float v)
        {
            // Tip at (0, 0.5); cone spans ±30° around +X and fades toward its far end.
            float dx = u;
            float dy = v - 0.5f;
            if (dx <= 0f) return 0f;
            float angle = Mathf.Abs(Mathf.Atan2(dy, dx)) * Mathf.Rad2Deg;
            float edge = 1f - Mathf.SmoothStep(24f, 30f, angle);
            float length = 1f - Mathf.SmoothStep(0.75f, 1f, Mathf.Sqrt(dx * dx + dy * dy));
            return edge * length;
        }
    }
}
