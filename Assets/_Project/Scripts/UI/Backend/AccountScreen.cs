using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Backend;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Backend
{
    /// <summary>
    /// Account & cloud save (EPIC 22 "Authentication", "Cloud Save"): connection status, user id, last
    /// sync time, "Sync now", optional email linking (so progress survives reinstalls), and the
    /// analytics opt-out.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu)]
    public class AccountScreen : UIScreen
    {
        public const string Id = "account";

        public override string ScreenId => Id;
        protected override string Title => "Account";

        private TextMeshProUGUI _status;
        private TMP_InputField _email;
        private TMP_InputField _password;
        private Button _analytics;
        private bool _busy;

        protected override void Build(RectTransform body)
        {
            _status = UIBuilder.Text(body, "", UITheme.BodySize);
            _status.richText = true;

            UIBuilder.Button(body, "Sync now", OnSync, UITheme.ButtonSecondary, 110f);

            UIBuilder.SectionHeader(body, "Protect your progress");
            UIBuilder.Text(body, "Link an email so your village follows you to a new phone.", UITheme.SmallSize, TextAlignmentOptions.Left, UITheme.TextMuted);
            _email = UIBuilder.InputField(body, "", "Email", 80);
            _email.contentType = TMP_InputField.ContentType.EmailAddress;
            _password = UIBuilder.InputField(body, "", "Password (6+ characters)", 64);
            _password.contentType = TMP_InputField.ContentType.Password;
            UIBuilder.Button(body, "Link email", OnLink, UITheme.Button, 110f);

            UIBuilder.SectionHeader(body, "Privacy");
            _analytics = UIBuilder.Button(body, "", () =>
            {
                AnalyticsService.OptedOut = !AnalyticsService.OptedOut;
                Sfx.Play(AudioCueIds.UiClick);
                Refresh();
            }, UITheme.ButtonSecondary, 100f);
        }

        private void OnEnable() => EventBus<BackendStatusChangedEvent>.Subscribe(OnStatus);
        private void OnDisable() => EventBus<BackendStatusChangedEvent>.Unsubscribe(OnStatus);
        private void OnStatus(BackendStatusChangedEvent evt) { if (IsVisible) Refresh(); }

        public override void Refresh()
        {
            var service = BackendService.Instance;
            var user = BackendService.Provider?.CurrentUser;
            string status = service != null ? service.StatusMessage : "Offline";
            string sync = service != null && service.LastCloudSyncUtc.HasValue
                ? service.LastCloudSyncUtc.Value.ToLocalTime().ToString("d MMM HH:mm")
                : "never";
            _status.text = $"<b>{status}</b>\nAccount: {(user != null ? user.UserId : "—")}" +
                           (user != null && !string.IsNullOrEmpty(user.Email) ? $" ({user.Email})" : user != null && user.IsAnonymous ? " (guest)" : string.Empty) +
                           $"\nLast cloud backup: {sync}";
            UIBuilder.SetLabel(_analytics, AnalyticsService.OptedOut ? "Share anonymous usage data: Off" : "Share anonymous usage data: On");
        }

        private async void OnSync()
        {
            if (_busy || BackendService.Instance == null) return;
            _busy = true;
            Sfx.Play(AudioCueIds.UiClick);
            await BackendService.Instance.ConnectAsync();
            _busy = false;
            if (this != null) Refresh();
        }

        private async void OnLink()
        {
            if (_busy || BackendService.Instance == null) return;
            string email = _email.text.Trim();
            if (!email.Contains("@") || _password.text.Length < 6)
            {
                Sfx.Play(AudioCueIds.UiError);
                UIScreenNavigator.Instance.Toast("Enter a valid email and a password of 6+ characters");
                return;
            }
            _busy = true;
            bool ok = await BackendService.Instance.LinkEmailAsync(email, _password.text);
            _busy = false;
            if (this == null) return;
            Sfx.Play(ok ? AudioCueIds.RewardClaim : AudioCueIds.UiError);
            UIScreenNavigator.Instance.Toast(ok ? "Account linked!" : "Linking needs an internet connection on a phone build");
            _password.SetTextWithoutNotify(string.Empty);
            Refresh();
        }
    }
}
