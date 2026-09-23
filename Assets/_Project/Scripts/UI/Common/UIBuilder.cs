using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NinjaVillage.UI.Common
{
    /// <summary>Placeholder palette for code-built screens — swap for real art/theme later in one place.</summary>
    public static class UITheme
    {
        public static readonly Color Backdrop = new(0.05f, 0.06f, 0.09f, 0.92f);
        public static readonly Color Panel = new(0.13f, 0.15f, 0.21f, 0.97f);
        public static readonly Color Card = new(0.19f, 0.22f, 0.30f, 1f);
        public static readonly Color Button = new(0.80f, 0.27f, 0.22f, 1f);   // ninja red
        public static readonly Color ButtonSecondary = new(0.30f, 0.34f, 0.45f, 1f);
        public static readonly Color ButtonDisabled = new(0.25f, 0.25f, 0.28f, 1f);
        public static readonly Color Text = new(0.96f, 0.94f, 0.88f, 1f);
        public static readonly Color TextMuted = new(0.70f, 0.70f, 0.74f, 1f);
        public static readonly Color Gold = new(1f, 0.80f, 0.25f, 1f);
        public static readonly Color Gem = new(0.45f, 0.85f, 1f, 1f);
        public static readonly Color Positive = new(0.40f, 0.85f, 0.45f, 1f);
        public static readonly Color Negative = new(0.95f, 0.35f, 0.35f, 1f);

        public const float TitleSize = 64f;
        public const float HeaderSize = 44f;
        public const float BodySize = 34f;
        public const float SmallSize = 26f;
        public const float ButtonHeight = 120f;
    }

    /// <summary>
    /// Builds uGUI + TextMeshPro elements from code. Used by the Main Menu, Village, Daily,
    /// Hero, Pet and Store screens so a scene only needs a <see cref="UIScreen"/> component —
    /// no hand-wired prefab hierarchy. Layout is driven by LayoutGroups; sizes are in
    /// reference pixels of the 1080×1920 portrait canvas.
    /// </summary>
    public static class UIBuilder
    {
        public static readonly Vector2 ReferenceResolution = new(1080f, 1920f);

        /// <summary>Screen-space canvas with the project's portrait scaler + raycaster; ensures an EventSystem exists.</summary>
        public static Canvas CreateCanvas(string name, int sortingOrder = 0, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();
            return canvas;
        }

        /// <summary>The project uses the new Input System only, so UI needs InputSystemUIInputModule.</summary>
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Object.DontDestroyOnLoad(go);
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Full-screen colored panel (also blocks raycasts to what's behind it).</summary>
        public static RectTransform Panel(Transform parent, string name, Color color, float inset = 0f)
        {
            var rt = Rect(parent, name);
            Stretch(rt, inset);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            return rt;
        }

        public static Image Image(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var rt = Rect(parent, name);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.preserveAspect = sprite != null;
            return image;
        }

        public static TextMeshProUGUI Text(Transform parent, string text, float size = UITheme.BodySize,
            TextAlignmentOptions align = TextAlignmentOptions.Left, Color? color = null, FontStyles style = FontStyles.Normal)
        {
            var rt = Rect(parent, "Text");
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = color ?? UITheme.Text;
            tmp.fontStyle = style;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button Button(Transform parent, string label, UnityAction onClick, Color? color = null,
            float height = UITheme.ButtonHeight, float fontSize = UITheme.BodySize)
        {
            var image = Image(parent, $"Button_{label}", color ?? UITheme.Button);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null) button.onClick.AddListener(onClick);

            var text = Text(image.transform, label, fontSize, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 8f);
            text.name = "Label";

            SetPreferredSize(image, -1f, height);
            return button;
        }

        /// <summary>Changes a button made by <see cref="Button"/>'s label text.</summary>
        public static void SetLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null) text.text = label;
        }

        /// <summary>Interactable + greyed-out look in one call.</summary>
        public static void SetEnabled(Button button, bool enabled, Color? enabledColor = null)
        {
            button.interactable = enabled;
            if (button.targetGraphic is Image image)
                image.color = enabled ? enabledColor ?? UITheme.Button : UITheme.ButtonDisabled;
        }

        public static VerticalLayoutGroup Vertical(Transform parent, string name, float spacing = 16f, int padding = 24,
            TextAnchor align = TextAnchor.UpperCenter)
        {
            var rt = Rect(parent, name);
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = align;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup Horizontal(Transform parent, string name, float spacing = 16f, int padding = 0,
            TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rt = Rect(parent, name);
            var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = align;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return layout;
        }

        /// <summary>A rounded "card" container with a vertical layout — the standard list row.</summary>
        public static VerticalLayoutGroup Card(Transform parent, string name, float spacing = 8f, int padding = 20)
        {
            var layout = Vertical(parent, name, spacing, padding, TextAnchor.UpperLeft);
            var image = layout.gameObject.AddComponent<Image>();
            image.color = UITheme.Card;
            return layout;
        }

        /// <summary>Vertical scroll view; add rows to <paramref name="content"/>.</summary>
        public static ScrollRect ScrollList(Transform parent, string name, out RectTransform content, float spacing = 16f)
        {
            var viewportParent = Rect(parent, name);
            var scroll = viewportParent.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var viewport = Rect(viewportParent, "Viewport");
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            // Transparent image so drags on empty space still scroll.
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);

            var layout = Vertical(viewport, "Content", spacing, 16);
            content = (RectTransform)layout.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            SetFlexible(viewportParent, 1f, 1f);
            return scroll;
        }

        /// <summary>Horizontal fill bar; returns the background, <paramref name="fill"/> is the filled Image (set fillAmount 0..1).</summary>
        public static Image ProgressBar(Transform parent, string name, out Image fill, Color? fillColor = null, float height = 28f)
        {
            var background = Image(parent, name, new Color(0f, 0f, 0f, 0.5f));
            fill = Image(background.transform, "Fill", fillColor ?? UITheme.Positive);
            Stretch(fill.rectTransform);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            // Filled images need a sprite to render the fill; use Unity's built-in white sprite.
            fill.sprite = WhiteSprite;
            SetPreferredSize(background, -1f, height);
            return background;
        }

        private static Sprite _whiteSprite;
        public static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite == null)
                {
                    var tex = Texture2D.whiteTexture;
                    _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
                return _whiteSprite;
            }
        }

        /// <summary>Fixed preferred size for layout groups; pass -1 to leave an axis unconstrained.</summary>
        public static LayoutElement SetPreferredSize(Component component, float width, float height)
        {
            if (!component.TryGetComponent<LayoutElement>(out var element)) element = component.gameObject.AddComponent<LayoutElement>();
            if (width >= 0f) element.preferredWidth = width;
            if (height >= 0f)
            {
                element.preferredHeight = height;
                element.minHeight = height;
            }
            return element;
        }

        public static LayoutElement SetFlexible(Component component, float flexibleWidth, float flexibleHeight)
        {
            if (!component.TryGetComponent<LayoutElement>(out var element)) element = component.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            return element;
        }

        public static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.Destroy(parent.GetChild(i).gameObject);
        }

        /// <summary>Compact "1.2K" / "3.4M" formatting for currency labels.</summary>
        public static string FormatAmount(long amount)
        {
            if (amount >= 1_000_000) return $"{amount / 1_000_000f:0.#}M";
            if (amount >= 10_000) return $"{amount / 1_000f:0.#}K";
            return amount.ToString();
        }
    }
}
