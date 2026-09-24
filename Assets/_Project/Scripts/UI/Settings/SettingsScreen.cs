using NinjaVillage.Core.Audio;
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
    /// Settings (EPIC 17 "Settings"): volume sliders applied live, vibration, 30 FPS battery saver,
    /// damage numbers, graphics quality, and "reset progress" behind a two-tap confirmation. All values
    /// go through <see cref="SettingsService"/>, which validates, applies and persists them.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu)]
    public class SettingsScreen : UIScreen
    {
        public override string ScreenId => ScreenIds.Settings;
        protected override string Title => "Settings";

        private Slider _master, _music, _sfx;
        private Button _vibration, _battery, _damageNumbers, _quality, _reset;
        private bool _confirmReset;

        protected override void Build(RectTransform body)
        {
            UIBuilder.SectionHeader(body, "Audio");
            _master = UIBuilder.Slider(body, "Master", 1f, SettingsService.SetMasterVolume);
            _music = UIBuilder.Slider(body, "Music", 1f, SettingsService.SetMusicVolume);
            _sfx = UIBuilder.Slider(body, "Effects", 1f, v =>
            {
                SettingsService.SetSfxVolume(v);
                Sfx.Play(AudioCueIds.UiClick); // preview (throttled by the audio manager)
            });

            UIBuilder.SectionHeader(body, "Gameplay");
            _vibration = UIBuilder.Button(body, "", () =>
            {
                SettingsService.SetVibration(!SettingsService.Current.Vibration);
                Click();
            }, UITheme.ButtonSecondary, 100f);
            _damageNumbers = UIBuilder.Button(body, "", () =>
            {
                SettingsService.SetShowDamageNumbers(!SettingsService.Current.ShowDamageNumbers);
                Click();
            }, UITheme.ButtonSecondary, 100f);

            UIBuilder.SectionHeader(body, "Performance");
            _battery = UIBuilder.Button(body, "", () =>
            {
                SettingsService.SetBatterySaver(!SettingsService.Current.BatterySaver);
                Click();
            }, UITheme.ButtonSecondary, 100f);
            _quality = UIBuilder.Button(body, "", () =>
            {
                int count = QualitySettings.names.Length;
                if (count > 0) SettingsService.SetQualityLevel((SettingsService.EffectiveQualityLevel + 1) % count);
                Click();
            }, UITheme.ButtonSecondary, 100f);

            UIBuilder.FlexibleSpacer(body);
            _reset = UIBuilder.Button(body, "", OnResetPressed, UITheme.ButtonSecondary, 100f);
            UIBuilder.Text(body, $"Ninja Village Defense v{Application.version}\nMade with Unity {Application.unityVersion}",
                UITheme.SmallSize, TextAlignmentOptions.Center, UITheme.TextMuted);
        }

        private void Click()
        {
            Sfx.Play(AudioCueIds.UiClick);
            Refresh();
        }

        public override void Refresh()
        {
            var s = SettingsService.Current;
            _master.SetValueWithoutNotify(s.MasterVolume);
            _music.SetValueWithoutNotify(s.MusicVolume);
            _sfx.SetValueWithoutNotify(s.SfxVolume);
            UIBuilder.SetLabel(_vibration, $"Vibration: {(s.Vibration ? "On" : "Off")}");
            UIBuilder.SetLabel(_damageNumbers, $"Damage numbers: {(s.ShowDamageNumbers ? "On" : "Off")}");
            UIBuilder.SetLabel(_battery, s.BatterySaver ? "Battery saver: On (30 FPS)" : "Battery saver: Off (60 FPS)");

            var names = QualitySettings.names;
            int level = SettingsService.EffectiveQualityLevel;
            UIBuilder.SetLabel(_quality, names.Length > 0 && level >= 0 && level < names.Length ? $"Graphics: {names[level]}" : "Graphics: Default");

            UIBuilder.SetLabel(_reset, _confirmReset ? "Tap again to ERASE all progress" : "Reset progress");
            UIBuilder.SetEnabled(_reset, true, _confirmReset ? UITheme.Negative : UITheme.ButtonSecondary);
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
