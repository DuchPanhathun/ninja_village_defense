using System.Collections.Generic;
using NinjaVillage.Core.Audio;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Save;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.MainMenu
{
    /// <summary>
    /// The Main Menu's home screen (EPIC 17 "Home"): title, currencies, best-run summary, the two big
    /// core-loop buttons (PLAY → Battle, VILLAGE → Village) and a grid linking every meta screen present
    /// in the scene. Buttons whose screen isn't in this scene are hidden, and screens with something to
    /// claim (<see cref="ScreenBadges"/>) get a "!" marker.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu)]
    public class HomeScreen : UIScreen
    {
        public override string ScreenId => ScreenIds.Home;

        private static readonly (string label, string screenId)[] Links =
        {
            ("Heroes", ScreenIds.Heroes),
            ("Pets", ScreenIds.Pets),
            ("Talents", ScreenIds.Talents),
            ("Gear", ScreenIds.Inventory),
            ("Daily Reward", ScreenIds.DailyLogin),
            ("Quests", ScreenIds.Quests),
            ("Achievements", ScreenIds.Achievements),
            ("Collection", ScreenIds.Collection),
            ("Battle Pass", ScreenIds.BattlePass),
            ("Events", ScreenIds.Events),
            ("Shop", ScreenIds.Store),
            ("Leaderboard", "leaderboard"),
            ("Account", "account"),
            ("Profile", ScreenIds.Profile),
            ("Settings", ScreenIds.Settings),
        };

        private readonly List<(Button button, string label, string screenId)> _links = new();
        private TextMeshProUGUI _summary;

        protected override void Build(RectTransform body)
        {
            CurrencyBar.Create(body);
            UIBuilder.Spacer(body, 30f);
            var title = UIBuilder.Text(body, "NINJA VILLAGE\nDEFENSE", UITheme.TitleSize * 1.3f, TextAlignmentOptions.Center, UITheme.Gold, FontStyles.Bold);
            UIBuilder.SetPreferredSize(title, -1f, 220f);
            UIBuilder.Text(body, "The last ninja protecting the hidden village.", UITheme.BodySize, TextAlignmentOptions.Center, UITheme.TextMuted);
            _summary = UIBuilder.Text(body, "", UITheme.SmallSize, TextAlignmentOptions.Center, UITheme.TextMuted);
            UIBuilder.Spacer(body, 20f);

            UIBuilder.Button(body, "PLAY", () => { Sfx.Play(AudioCueIds.UiClick); SceneLoader.LoadBattle(); }, UITheme.Button, 170f, UITheme.TitleSize);
            UIBuilder.Button(body, "VILLAGE", () => { Sfx.Play(AudioCueIds.UiClick); SceneLoader.LoadVillage(); }, UITheme.ButtonSecondary, 140f, UITheme.HeaderSize);

            var grid = UIBuilder.Rect(body, "Links");
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(320f, 110f);
            layout.spacing = new Vector2(16f, 16f);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            UIBuilder.SetFlexible(grid, 1f, 1f);

            foreach (var (label, screenId) in Links)
            {
                string id = screenId;
                var button = UIBuilder.Button(grid, label, () =>
                {
                    Sfx.Play(AudioCueIds.UiClick);
                    UIScreenNavigator.Instance.Show(id);
                }, UITheme.ButtonSecondary, 110f, UITheme.SmallSize + 4f);
                _links.Add((button, label, id));
            }

            UIBuilder.Text(body, $"v{Application.version}", UITheme.SmallSize, TextAlignmentOptions.Right, UITheme.TextMuted);
        }

        protected override void Start()
        {
            base.Start();
            UIScreenNavigator.Instance.ShowRoot(ScreenId);
        }

        public override void Refresh()
        {
            var profile = SaveService.Data.Profile;
            _summary.text = profile.TotalRuns > 0
                ? $"{profile.DisplayName} · Best wave {profile.HighestWaveReached} · {profile.TotalRuns} runs · {profile.TotalKills} demons defeated"
                : $"Welcome, {profile.DisplayName}. Your village needs you!";

            var navigator = UIScreenNavigator.Instance;
            foreach (var (button, label, screenId) in _links)
            {
                bool present = navigator.Has(screenId);
                button.gameObject.SetActive(present);
                if (present) UIBuilder.SetLabel(button, ScreenBadges.Has(screenId) ? label + " !" : label);
            }
        }
    }
}
