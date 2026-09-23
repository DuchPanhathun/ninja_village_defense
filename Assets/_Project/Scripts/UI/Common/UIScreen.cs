using NinjaVillage.Core.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Common
{
    /// <summary>
    /// Base for code-built full-screen UI (menus, village screens, popups). Put the component
    /// on any GameObject in a scene; on Awake it builds its content under a <see cref="UIScreenNavigator"/>
    /// canvas (created on demand) and registers itself by <see cref="ScreenId"/>.
    ///
    /// Subclasses implement <see cref="Build"/> once (static layout) and <see cref="Refresh"/>
    /// (re-read data; called every time the screen is shown and whenever the subclass wants).
    /// Open screens with <c>UIScreenNavigator.Show("id")</c>; the Back button / Android back pops.
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        [Tooltip("Shown on scene start (the scene's home screen).")]
        [SerializeField] private bool showOnStart;

        /// <summary>Unique per scene, e.g. "village.forge". Used by <see cref="UIScreenNavigator.Show"/>.</summary>
        public abstract string ScreenId { get; }

        /// <summary>Title in the header bar. Return null for no header (e.g. a home screen draws its own).</summary>
        protected virtual string Title => null;

        /// <summary>Whether the header shows a Back button.</summary>
        protected virtual bool ShowBackButton => true;

        /// <summary>Popups draw over the previous screen instead of replacing it.</summary>
        public virtual bool IsPopup => false;

        public RectTransform Root { get; private set; }
        /// <summary>Area below the header where <see cref="Build"/> puts content (has a VerticalLayoutGroup).</summary>
        protected RectTransform Body { get; private set; }
        protected TextMeshProUGUI TitleText { get; private set; }

        public bool IsVisible => Root != null && Root.gameObject.activeSelf;

        protected virtual void Awake()
        {
            var navigator = UIScreenNavigator.Instance;
            Root = UIBuilder.Panel(navigator.CanvasTransform, $"Screen_{ScreenId}", IsPopup ? new Color(0, 0, 0, 0.7f) : UITheme.Backdrop);

            RectTransform container = Root;
            if (IsPopup)
            {
                // Centered card on a dimmed backdrop.
                container = UIBuilder.Panel(Root, "PopupCard", UITheme.Panel);
                container.anchorMin = new Vector2(0.06f, 0.2f);
                container.anchorMax = new Vector2(0.94f, 0.8f);
                container.offsetMin = container.offsetMax = Vector2.zero;
            }

            var column = UIBuilder.Vertical(container, "Column", 12f, 0);
            UIBuilder.Stretch((RectTransform)column.transform);
            column.childForceExpandHeight = false;

            if (Title != null)
                BuildHeader(column.transform);

            // Body is a vertical layout: screens just append rows/cards/scroll lists to it.
            // A ScrollList added to it flexes to fill the remaining height.
            Body = (RectTransform)UIBuilder.Vertical(column.transform, "Body", 16f, 24).transform;
            UIBuilder.SetFlexible(Body, 1f, 1f);

            Build(Body);
            Root.gameObject.SetActive(false);
            navigator.Register(this);
        }

        protected virtual void Start()
        {
            if (showOnStart) UIScreenNavigator.Instance.Show(ScreenId);
        }

        protected virtual void OnDestroy()
        {
            if (UIScreenNavigator.HasInstance) UIScreenNavigator.Instance.Unregister(this);
        }

        private void BuildHeader(Transform parent)
        {
            var header = UIBuilder.Horizontal(parent, "Header", 16f, 16, TextAnchor.MiddleLeft);
            header.childForceExpandWidth = false;
            header.gameObject.AddComponent<Image>().color = UITheme.Panel;
            UIBuilder.SetPreferredSize(header, -1f, 150f);

            if (ShowBackButton)
            {
                var back = UIBuilder.Button(header.transform, "<", OnBackPressed, UITheme.ButtonSecondary, 110f, UITheme.HeaderSize);
                UIBuilder.SetPreferredSize(back, 110f, 110f);
            }

            TitleText = UIBuilder.Text(header.transform, Title, UITheme.HeaderSize, TextAlignmentOptions.Left, UITheme.Text, FontStyles.Bold);
            UIBuilder.SetFlexible(TitleText, 1f, 0f);
        }

        /// <summary>Called once from Awake. Lay out static content under <paramref name="body"/>.</summary>
        protected abstract void Build(RectTransform body);

        /// <summary>Re-read data and update labels. Called on every show.</summary>
        public virtual void Refresh() { }

        internal void SetVisible(bool visible)
        {
            Root.gameObject.SetActive(visible);
            if (visible)
            {
                Root.SetAsLastSibling();
                Refresh();
                OnShown();
            }
            else
            {
                OnHidden();
            }
        }

        protected virtual void OnShown() { }
        protected virtual void OnHidden() { }

        protected virtual void OnBackPressed()
        {
            Sfx.Play(AudioCueIds.UiBack);
            UIScreenNavigator.Instance.Back();
        }
    }
}
