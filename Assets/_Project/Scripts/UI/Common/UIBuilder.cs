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
            // Expand: the whole 1080x1920 design always fits — tall phones get extra height, tablets extra
            // width. (Blending width/height made tall phones' canvas narrower than 1080, clipping the right edge.)
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            EnsureEventSystem();
            return canvas;
        }

        /// <summary>The project uses the new Input System only, so UI needs InputSystemUIInputModule.</summary>
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || Object.FindAnyObjectByType<EventSystem>() != null) return;
            // Scene-local on purpose: a persistent one would duplicate the EventSystem already in Battle.unity.
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
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

        /// <summary>
        /// The standard list entry: a card with a bold title, a muted multi-line body and a row for
        /// action buttons (returned). <paramref name="accent"/> tints the title (rarity, hero theme...).
        /// </summary>
        public static HorizontalLayoutGroup ActionCard(Transform parent, string title, string body,
            out TextMeshProUGUI titleText, out TextMeshProUGUI bodyText, Color? accent = null)
        {
            var card = Card(parent, "Card_" + title, 10f, 22);
            titleText = Text(card.transform, title, UITheme.HeaderSize * 0.85f, TextAlignmentOptions.Left, accent ?? UITheme.Text, FontStyles.Bold);
            bodyText = Text(card.transform, body, UITheme.SmallSize, TextAlignmentOptions.Left, UITheme.TextMuted);
            bodyText.richText = true;
            if (string.IsNullOrEmpty(body)) bodyText.gameObject.SetActive(false);

            var actions = Horizontal(card.transform, "Actions", 12f, 0, TextAnchor.MiddleRight);
            actions.childForceExpandWidth = false;
            SetPreferredSize(actions, -1f, 96f);
            return actions;
        }

        /// <summary>Compact button for card action rows.</summary>
        public static Button SmallButton(Transform parent, string label, UnityAction onClick, Color? color = null, float width = 260f)
        {
            var button = Button(parent, label, onClick, color, 90f, UITheme.SmallSize + 2f);
            SetPreferredSize(button, width, 90f);
            return button;
        }

        /// <summary>Bold section heading inside a list ("Offense", "Daily Quests"...).</summary>
        public static TextMeshProUGUI SectionHeader(Transform parent, string text)
        {
            var header = Text(parent, text, UITheme.HeaderSize, TextAlignmentOptions.Left, UITheme.Gold, FontStyles.Bold);
            SetPreferredSize(header, -1f, 70f);
            return header;
        }

        /// <summary>Empty element that soaks up the remaining space in a layout (pushes siblings apart).</summary>
        public static RectTransform FlexibleSpacer(Transform parent)
        {
            var rt = Rect(parent, "Spacer");
            SetFlexible(rt, 1f, 1f);
            return rt;
        }

        /// <summary>Fixed-height gap.</summary>
        public static RectTransform Spacer(Transform parent, float height)
        {
            var rt = Rect(parent, "Gap");
            SetPreferredSize(rt, -1f, height);
            return rt;
        }

        /// <summary>Labeled 0..1 slider row ("Music ────●──"). <paramref name="onChanged"/> fires while dragging.</summary>
        public static Slider Slider(Transform parent, string label, float value, UnityAction<float> onChanged)
        {
            var row = Horizontal(parent, "Slider_" + label, 20f, 0, TextAnchor.MiddleLeft);
            row.childForceExpandWidth = false;
            SetPreferredSize(row, -1f, 90f);
            var text = Text(row.transform, label, UITheme.BodySize);
            SetPreferredSize(text, 280f, 90f);

            var root = Rect(row.transform, "Slider");
            SetFlexible(root, 1f, 0f);
            SetPreferredSize(root, -1f, 60f);
            var slider = root.gameObject.AddComponent<Slider>();

            var background = Image(root, "Background", new Color(0f, 0f, 0f, 0.45f));
            Stretch(background.rectTransform);
            background.rectTransform.offsetMin = new Vector2(0f, 18f);
            background.rectTransform.offsetMax = new Vector2(0f, -18f);

            var fillArea = Rect(root, "Fill Area");
            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(10f, 18f);
            fillArea.offsetMax = new Vector2(-10f, -18f);
            var fill = Image(fillArea, "Fill", UITheme.Button);
            fill.rectTransform.sizeDelta = Vector2.zero;

            var handleArea = Rect(root, "Handle Slide Area");
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(20f, 0f);
            handleArea.offsetMax = new Vector2(-20f, 0f);
            var handle = Image(handleArea, "Handle", UITheme.Text, WhiteSprite);
            handle.rectTransform.sizeDelta = new Vector2(44f, 0f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
            if (onChanged != null) slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        /// <summary>Single-line text input with a placeholder (TMP_InputField built from code).</summary>
        public static TMP_InputField InputField(Transform parent, string text, string placeholder, int characterLimit = 20)
        {
            var background = Image(parent, "InputField", new Color(0f, 0f, 0f, 0.45f));
            SetPreferredSize(background, -1f, 100f);
            var input = background.gameObject.AddComponent<TMP_InputField>();

            var area = Rect(background.transform, "Text Area");
            Stretch(area, 16f);
            area.gameObject.AddComponent<RectMask2D>();

            var placeholderText = Text(area, placeholder, UITheme.BodySize, TextAlignmentOptions.Left, UITheme.TextMuted, FontStyles.Italic);
            Stretch(placeholderText.rectTransform);
            placeholderText.textWrappingMode = TextWrappingModes.NoWrap;

            var valueText = Text(area, string.Empty, UITheme.BodySize, TextAlignmentOptions.Left);
            Stretch(valueText.rectTransform);
            valueText.textWrappingMode = TextWrappingModes.NoWrap;

            input.textViewport = area;
            input.textComponent = valueText;
            input.placeholder = placeholderText;
            input.pointSize = UITheme.BodySize;
            input.characterLimit = characterLimit;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.SetTextWithoutNotify(text ?? string.Empty);
            return input;
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
