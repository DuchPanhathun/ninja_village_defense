using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Backend;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Settings;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Settings
{
    /// <summary>
    /// Settings (EPIC 17 "Settings" + EPIC 22 "Account"), in one scrolling list: the account (online status,
    /// guest or linked email, last cloud backup, "Sync now", and linking an email so progress survives a
    /// reinstall), volume sliders applied live, vibration, damage numbers, screen shake, 30 FPS battery saver, graphics
    /// quality, the analytics opt-out, and "reset progress" behind a two-tap confirmation. Game settings go
    /// through <see cref="SettingsService"/>, which validates, applies and persists them.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu)]
    public class SettingsScreen : UIScreen
    {
        public override string ScreenId => ScreenIds.Settings;
        protected override string Title => "Settings";

        private Slider _master, _music, _sfx;
        private Button _vibration, _battery, _damageNumbers, _screenShake, _battleView, _quality, _analytics, _reset;
        private TextMeshProUGUI _accountStatus, _linkHint;
        private TMP_InputField _email, _password;
        private GameObject _linkForm;
        private bool _confirmReset;
        private bool _busy;

        protected override void Build(RectTransform body)
        {
            UIBuilder.ScrollList(body, "Settings", out var list, 12f);

            BuildAccount(list);

            UIBuilder.SectionHeader(list, "Audio");
            _master = UIBuilder.Slider(list, "Master", 1f, SettingsService.SetMasterVolume);
            _music = UIBuilder.Slider(list, "Music", 1f, SettingsService.SetMusicVolume);
            _sfx = UIBuilder.Slider(list, "Effects", 1f, v =>
            {
                SettingsService.SetSfxVolume(v);
                Sfx.Play(AudioCueIds.UiClick); // preview (throttled by the audio manager)
            });

            UIBuilder.SectionHeader(list, "Gameplay");
            _vibration = UIBuilder.Button(list, "", () =>
            {
                SettingsService.SetVibration(!SettingsService.Current.Vibration);
                Click();
            }, UITheme.ButtonSecondary, 100f);
            _damageNumbers = UIBuilder.Button(list, "", () =>
            {
                SettingsService.SetShowDamageNumbers(!SettingsService.Current.ShowDamageNumbers);
                Click();
            }, UITheme.ButtonSecondary, 100f);
            _screenShake = UIBuilder.Button(list, "", () =>
            {
                SettingsService.SetScreenShake(!SettingsService.Current.ScreenShake);
                Click();
            }, UITheme.ButtonSecondary, 100f);
            _battleView = UIBuilder.Button(list, "", () =>
            {
                SettingsService.SetBattleZoom(SettingsSaveData.NextBattleZoom(SettingsService.Current.BattleZoom));
                Click();
            }, UITheme.ButtonSecondary, 100f);
            _battleView.name = "BattleView";

            UIBuilder.SectionHeader(list, "Performance");
            _battery = UIBuilder.Button(list, "", () =>
            {
                SettingsService.SetBatterySaver(!SettingsService.Current.BatterySaver);
                Click();
            }, UITheme.ButtonSecondary, 100f);
            _quality = UIBuilder.Button(list, "", () =>
            {
                int count = QualitySettings.names.Length;
                if (count > 0) SettingsService.SetQualityLevel((SettingsService.EffectiveQualityLevel + 1) % count);
                Click();
            }, UITheme.ButtonSecondary, 100f);

            UIBuilder.SectionHeader(list, "Privacy");
            _analytics = UIBuilder.Button(list, "", () =>
            {
                AnalyticsService.OptedOut = !AnalyticsService.OptedOut;
                Click();
            }, UITheme.ButtonSecondary, 100f);

            UIBuilder.Spacer(list, 30f);
            _reset = UIBuilder.Button(list, "", OnResetPressed, UITheme.ButtonSecondary, 100f);
            UIBuilder.Text(list, $"Ninja Village Defense v{Application.version}\nMade with Unity {Application.unityVersion}",
                UITheme.SmallSize, TextAlignmentOptions.Center, UITheme.TextMuted);
        }

        /// <summary>Status + Sync now, and the email link form (replaced by "Linked to ..." once linked).</summary>
        private void BuildAccount(RectTransform list)
        {
            UIBuilder.SectionHeader(list, "Account");
            _accountStatus = UIBuilder.Text(list, "", UITheme.SmallSize, TextAlignmentOptions.Left);
            _accountStatus.richText = true;
            UIBuilder.Button(list, "Sync now", OnSync, UITheme.ButtonSecondary, 100f);

            _linkHint = UIBuilder.Text(list, "", UITheme.SmallSize, TextAlignmentOptions.Left, UITheme.TextMuted);
            _linkHint.richText = true;
            var form = UIBuilder.Vertical(list, "LinkForm", 12f, 0);
            _linkForm = form.gameObject;
            _email = UIBuilder.InputField(form.transform, "", "Email", 80);
            _email.contentType = TMP_InputField.ContentType.EmailAddress;
            _password = UIBuilder.InputField(form.transform, "", "Password (6+ characters)", 64);
            _password.contentType = TMP_InputField.ContentType.Password;
            UIBuilder.Button(form.transform, "Link email", OnLink, UITheme.Button, 100f);
        }

        private void Click()
        {
            Sfx.Play(AudioCueIds.UiClick);
            Refresh();
        }

        private void OnEnable() => EventBus<BackendStatusChangedEvent>.Subscribe(OnStatus);
        private void OnDisable() => EventBus<BackendStatusChangedEvent>.Unsubscribe(OnStatus);
        private void OnStatus(BackendStatusChangedEvent evt) { if (IsVisible) Refresh(); }

        public override void Refresh()
        {
            RefreshAccount();

            var s = SettingsService.Current;
            _master.SetValueWithoutNotify(s.MasterVolume);
            _music.SetValueWithoutNotify(s.MusicVolume);
            _sfx.SetValueWithoutNotify(s.SfxVolume);
            UIBuilder.SetLabel(_vibration, $"Vibration: {(s.Vibration ? "On" : "Off")}");
            UIBuilder.SetLabel(_damageNumbers, $"Damage numbers: {(s.ShowDamageNumbers ? "On" : "Off")}");
            UIBuilder.SetLabel(_screenShake, $"Screen shake: {(s.ScreenShake ? "On" : "Off")}");
            UIBuilder.SetLabel(_battleView, $"Battle view: {SettingsSaveData.DescribeBattleZoom(s.BattleZoom)}");
            UIBuilder.SetLabel(_battery, s.BatterySaver ? "Battery saver: On (30 FPS)" : "Battery saver: Off (60 FPS)");

            var names = QualitySettings.names;
            int level = SettingsService.EffectiveQualityLevel;
            UIBuilder.SetLabel(_quality, names.Length > 0 && level >= 0 && level < names.Length ? $"Graphics: {names[level]}" : "Graphics: Default");
            UIBuilder.SetLabel(_analytics, AnalyticsService.OptedOut ? "Share anonymous usage data: Off" : "Share anonymous usage data: On");

            UIBuilder.SetLabel(_reset, _confirmReset ? "Tap again to ERASE all progress" : "Reset progress");
            UIBuilder.SetEnabled(_reset, true, _confirmReset ? UITheme.Negative : UITheme.ButtonSecondary);
        }

        private void RefreshAccount()
        {
            var service = BackendService.Instance;
            var user = BackendService.Provider?.CurrentUser;
            string status = service != null ? service.StatusMessage : "Offline";
            string sync = service != null && service.LastCloudSyncUtc.HasValue
                ? service.LastCloudSyncUtc.Value.ToLocalTime().ToString("d MMM HH:mm")
                : "never";
            bool linked = user != null && !string.IsNullOrEmpty(user.Email);
            _accountStatus.text = $"<b>{status}</b>\n" +
                                  (user == null ? "Not signed in" : linked ? $"Signed in as {user.Email}" : "Playing as a guest") +
                                  $"\nLast cloud backup: {sync}";
            _linkHint.text = linked
                ? $"<color=#9CFF8A>Linked to {user.Email}</color> — your village follows you to a new phone."
                : "Link an email so your village follows you to a new phone (a guest account is lost if the game is uninstalled).";
            _linkForm.SetActive(!linked);
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
            UIScreenNavigator.Instance.Toast(ok ? "Account linked!" : "Couldn't link — check your connection, or the email may already be in use");
            _password.SetTextWithoutNotify(string.Empty);
            Refresh();
        }

        private void OnResetPressed()
        {
            if (!_confirmReset)
            {
                _confirmReset = true;
                Sfx.Play(AudioCueIds.UiError);
                Refresh();
                return;
            }

            _confirmReset = false;
            SaveService.ResetAll();
            SettingsService.ApplyAll();
            SceneLoader.ReloadActive(); // every screen/system re-reads the fresh save
        }

        protected override void OnHidden()
        {
            _confirmReset = false;
            SettingsService.Flush();
        }
    }
}
