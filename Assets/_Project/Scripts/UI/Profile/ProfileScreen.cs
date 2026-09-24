using System;
using NinjaVillage.Core.Audio;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Profile
{
    /// <summary>
    /// Player profile (EPIC 16 "Player profile"): editable display name (shown on leaderboards),
    /// lifetime stats and the recent run history recorded by the GameManager.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu)]
    public class ProfileScreen : UIListScreen
    {
        public const int MaxNameLength = 16;

        public override string ScreenId => ScreenIds.Profile;
        protected override string Title => "Profile";

        private TMP_InputField _nameField;

        protected override void BuildTop(RectTransform body)
        {
            _nameField = UIBuilder.InputField(body, SaveService.Data.Profile.DisplayName, "Your ninja name", MaxNameLength);
            _nameField.onEndEdit.AddListener(OnNameEdited);
        }

        /// <summary>Trims, strips rich-text brackets and caps the length; empty → "Ninja".</summary>
        public static string SanitizeName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Ninja";
            string cleaned = raw.Replace("<", string.Empty).Replace(">", string.Empty).Trim();
            if (cleaned.Length > MaxNameLength) cleaned = cleaned.Substring(0, MaxNameLength);
            return cleaned.Length == 0 ? "Ninja" : cleaned;
        }

        private void OnNameEdited(string value)
        {
            var profile = SaveService.Data.Profile;
            string name = SanitizeName(value);
            _nameField.SetTextWithoutNotify(name);
            if (name == profile.DisplayName) return;
            profile.DisplayName = name;
            SaveService.SaveNow();
            Sfx.Play(AudioCueIds.UiClick);
            UIScreenNavigator.Instance.Toast("Name saved");
        }

        protected override void Populate(RectTransform content)
        {
            var p = SaveService.Data.Profile;
            _nameField.SetTextWithoutNotify(p.DisplayName);
            InfoText.text = $"Player ID: {p.PlayerId}\nPlaying since {new DateTime(p.CreatedUtcTicks, DateTimeKind.Utc).ToLocalTime():d MMM yyyy}";

            UIBuilder.SectionHeader(content, "Lifetime");
            UIBuilder.ActionCard(content, "Battles",
                $"Runs: {p.TotalRuns} · Victories: {p.TotalVictories}\n" +
                $"Best wave: {p.HighestWaveReached} · Longest run: {FormatDuration(p.LongestRunSeconds)}\n" +
                $"Demons defeated: {p.TotalKills} · Bosses: {p.TotalBossesKilled}\n" +
                $"Coins earned: {p.TotalCoinsEarned}", out _, out _);

            UIBuilder.SectionHeader(content, "Recent runs");
            if (p.RecentRuns.Count == 0)
            {
                UIBuilder.Text(content, "No runs yet — press PLAY!", UITheme.BodySize, TextAlignmentOptions.Left, UITheme.TextMuted);
                return;
            }

            foreach (var run in p.RecentRuns)
            {
                if (run == null) continue;
                string when = new DateTime(run.EndedUtcTicks, DateTimeKind.Utc).ToLocalTime().ToString("d MMM HH:mm");
                string title = $"{(run.Victory ? "Victory" : "Defeated")} — wave {run.WaveReached}";
                string body = $"{when} · {FormatDuration(run.DurationSeconds)} · {run.Kills} kills · +{run.CoinsEarned} coins" +
                              (string.IsNullOrEmpty(run.HeroId) ? string.Empty : $"\nHero: {run.HeroId}") +
                              (string.IsNullOrEmpty(run.WeaponId) ? string.Empty : $" · Weapon: {run.WeaponId}");
                UIBuilder.ActionCard(content, title, body, out _, out _, run.Victory ? UITheme.Positive : UITheme.Text);
            }
        }

        public static string FormatDuration(float seconds)
        {
            int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return s >= 3600 ? $"{s / 3600}h {s % 3600 / 60}m" : $"{s / 60}m {s % 60:00}s";
        }
    }
}
