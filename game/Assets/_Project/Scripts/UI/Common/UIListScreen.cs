using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Common
{
    /// <summary>
    /// Shared layout for list-style meta screens: currency bar, an info line, then a scrolling list
    /// of cards that is rebuilt from data on every <see cref="Refresh"/>. Subclasses only describe the
    /// cards in <see cref="Populate"/> and call <see cref="Refresh"/> after an action changes data.
    /// </summary>
    public abstract class UIListScreen : UIScreen
    {
        protected TextMeshProUGUI InfoText { get; private set; }
        protected RectTransform Content { get; private set; }
        private ScrollRect _scroll;

        protected override void Build(RectTransform body)
        {
            CurrencyBar.Create(body);
            InfoText = UIBuilder.Text(body, "", UITheme.SmallSize, TextAlignmentOptions.Left, UITheme.TextMuted);
            InfoText.richText = true;
            BuildTop(body);
            _scroll = UIBuilder.ScrollList(body, "List", out var content);
            Content = content;
            BuildBottom(body);
        }

        /// <summary>Optional fixed controls between the info line and the list (tabs, reset buttons...).</summary>
        protected virtual void BuildTop(RectTransform body) { }

        /// <summary>Optional fixed controls under the list.</summary>
        protected virtual void BuildBottom(RectTransform body) { }

        /// <summary>Fill <paramref name="content"/> with cards (it is cleared first). Set InfoText here too.</summary>
        protected abstract void Populate(RectTransform content);

        public override void Refresh()
        {
            if (Content == null) return;
            UIBuilder.ClearChildren(Content);
            Populate(Content);
        }

        protected override void OnShown()
        {
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;
        }

        protected static void Toast(string message)
        {
            if (!string.IsNullOrEmpty(message)) UIScreenNavigator.Instance.Toast(message);
        }
    }
}
