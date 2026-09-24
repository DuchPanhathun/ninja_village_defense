using System.Collections.Generic;
using NinjaVillage.Systems.Backend;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Backend
{
    /// <summary>
    /// Global leaderboard (EPIC 22): top 50 by best wave (kills break ties), the player's own row
    /// highlighted. Offline (Editor / no network) it shows the player's own best with an explanation.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu)]
    public class LeaderboardScreen : UIListScreen
    {
        public const string Id = "leaderboard";

        public override string ScreenId => Id;
        protected override string Title => "Leaderboard";

        private List<LeaderboardEntry> _entries;
        private bool _loading;

        protected override async void OnShown()
        {
            base.OnShown();
            if (_loading) return;
            _loading = true;
            _entries = null;
            Refresh();
            _entries = await LeaderboardService.FetchTopAsync();
            _loading = false;
            if (this != null && IsVisible) Refresh();
        }

        protected override void Populate(RectTransform content)
        {
            var provider = BackendService.Provider;
            bool online = provider != null && provider.IsOnline;
            InfoText.text = online
                ? $"Your best: wave {LeaderboardService.BestWave} ({LeaderboardService.BestKills} kills)"
                : "Offline — the global leaderboard is available on phone builds with an internet connection.";

            if (_entries == null)
            {
                UIBuilder.Text(content, "Loading...", UITheme.BodySize, TextAlignmentOptions.Center, UITheme.TextMuted);
                return;
            }
            if (_entries.Count == 0)
            {
                UIBuilder.Text(content, "No scores yet — be the first!", UITheme.BodySize, TextAlignmentOptions.Center, UITheme.TextMuted);
                return;
            }

            string myId = provider?.CurrentUser?.UserId;
            foreach (var entry in _entries)
            {
                bool mine = !string.IsNullOrEmpty(myId) && entry.UserId == myId;
                UIBuilder.ActionCard(content, $"#{entry.Rank}  {entry.DisplayName ?? "Ninja"}{(mine ? "  (you)" : string.Empty)}",
                    $"Wave {entry.BestWave} · {entry.BestKills} kills", out _, out _, mine ? UITheme.Gold : UITheme.Text);
            }
        }
    }
}
