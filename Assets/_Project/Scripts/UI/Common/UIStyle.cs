using NinjaVillage.Core.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NinjaVillage.UI.Common
{
    /// <summary>
    /// The chunky pixel-art look used by the home screen (and available to any screen): 9-sliced wood
    /// panels from the Ninja Adventure UI kit, bold outlined text, square icon buttons with a label and
    /// a red "!" badge. Everything falls back to flat colours when <see cref="UIArt"/> is missing.
    /// </summary>
    public static class UIStyle
    {
        public static readonly Color Outline = new(0.08f, 0.1f, 0.1f, 1f);   // the pack's #141b1b
        public static readonly Color Cream = new(1f, 0.95f, 0.85f, 1f);
        public static readonly Color BadgeRed = new(0.88f, 0.22f, 0.3f, 1f);

        /// <summary>An Image with a UIArt sprite: 9-sliced when the sprite has borders, else simple.</summary>
        public static Image Sprite(Transform parent, string name, string spriteName, Color? fallback = null)
        {
            var sprite = UIArt.Get(spriteName);
            var image = UIBuilder.Image(parent, name, sprite != null ? Color.white : fallback ?? UITheme.Panel, sprite);
            if (sprite != null && sprite.border != Vector4.zero)
            {
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
            }
            return image;
        }

        /// <summary>Bold text with the pack's dark outline — the cartoon-game look.</summary>
        public static TextMeshProUGUI Chunky(TextMeshProUGUI text, float outline = 0.22f)
        {
            text.fontStyle |= FontStyles.Bold;
            text.outlineWidth = outline;
            text.outlineColor = Outline;
            return text;
        }

        public static TextMeshProUGUI Label(Transform parent, string value, float size, Color? color = null,
            TextAlignmentOptions align = TextAlignmentOptions.Center, float outline = 0.22f)
        {
            var text = UIBuilder.Text(parent, value, size, align, color ?? Color.white);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            return Chunky(text, outline);
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        /// <summary>A clickable wood frame; returns the button (its Image is the frame).</summary>
        public static Button Frame(Transform parent, string name, string frameSprite, UnityAction onClick, Color? fallback = null)
        {
            var image = Sprite(parent, name, frameSprite, fallback);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            button.colors = colors;
            if (onClick != null)
                button.onClick.AddListener(() =>
                {
                    Sfx.Play(AudioCueIds.UiClick);
                    onClick();
                });
            return button;
        }

        /// <summary>
        /// Square icon button (icon on a wood tile) with an outlined label underneath and a hidden badge.
        /// Icons that already have their own tile (Ludo/PixelLab menu icons) skip the wood tile.
        /// </summary>
        public static IconButton Icon(Transform parent, string iconSprite, string label, UnityAction onClick, float size = 150f,
            bool tile = true)
        {
            var root = UIBuilder.Rect(parent, $"Icon_{label}");
            root.sizeDelta = new Vector2(size, size + 44f);

            var frame = Frame(root, "Tile", tile ? "panel_wood_panel_3" : null, onClick, tile ? UITheme.ButtonSecondary : Color.clear);
            var frameRt = (RectTransform)frame.transform;
            Place(frameRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(size, size));
            if (!tile) frame.image.color = new Color(1f, 1f, 1f, 0f); // invisible but clickable

            var icon = UIBuilder.Image(frame.transform, "Icon", Color.white, UIArt.Get(iconSprite));
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            UIBuilder.Stretch(icon.rectTransform, tile ? size * 0.16f : 0f);

            var text = Label(root, label, 30f);
            Place(text.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(size + 40f, 44f));

            var badge = Badge(frame.transform, size);
            return new IconButton(root, frame, badge);
        }

        /// <summary>Red "!" circle in the top-right corner, hidden by default.</summary>
        public static GameObject Badge(Transform parent, float parentSize)
        {
            float d = Mathf.Clamp(parentSize * 0.32f, 36f, 56f);
            var badge = UIBuilder.Image(parent, "Badge", BadgeRed, Core.Utilities.GeneratedSprites.Circle);
            badge.raycastTarget = false;
            Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-d * 0.15f, -d * 0.15f), new Vector2(d, d));
            var mark = Label(badge.transform, "!", d * 0.72f, Color.white, TextAlignmentOptions.Center, 0.15f);
            UIBuilder.Stretch(mark.rectTransform);
            badge.gameObject.SetActive(false);
            return badge.gameObject;
        }

        public readonly struct IconButton
        {
            public readonly RectTransform Root;
            public readonly Button Button;
            public readonly GameObject Badge;

            public IconButton(RectTransform root, Button button, GameObject badge)
            {
                Root = root;
                Button = button;
                Badge = badge;
            }
        }
    }
}
