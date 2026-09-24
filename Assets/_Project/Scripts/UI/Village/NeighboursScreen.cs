using System.Collections.Generic;
using NinjaVillage.Systems.Backend;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Village
{
    /// <summary>
    /// Neighbours: the most recently active villages of other players (from Firebase), each with its owner's
    /// castle, best wave, chapters and trophies, and a Visit button that opens that village read-only.
    /// </summary>
    [SceneScreen(SceneNames.Village, SceneNames.MainMenu)]
    public class NeighboursScreen : UIListScreen
    {
        public const int Count = 30;

        public override string ScreenId => ScreenIds.Neighbours;
        protected override string Title => "Neighbours";

        private List<PublicVillage> _villages;
        private bool _loading;

        protected override async void OnShown()
        {
            base.OnShown();
            if (_loading) return;
            _loading = true;
            _villages = null;
            Refresh();
            _villages = await BackendService.ListVillagesAsync(Count + 1);
            _loading = false;
            if (this != null && IsVisible) Refresh();
        }

        protected override void Populate(RectTransform content)
        {
            var provider = BackendService.Provider;
            bool online = provider != null && provider.IsOnline;
            InfoText.text = online
                ? "Villages of ninjas who played recently. Visit to see their buildings, heroes, pets and decorations."
                : "Offline — neighbours appear on phone builds with an internet connection.";

            if (_villages == null)
            {
                UIBuilder.Text(content, "Loading...", UITheme.BodySize, TextAlignmentOptions.Center, UITheme.TextMuted);
                return;
            }

            string myId = provider?.CurrentUser?.UserId;
            int shown = 0;
            foreach (var village in _villages)
            {
                if (village == null || village.UserId == myId || shown >= Count) continue;
                shown++;
                string name = string.IsNullOrEmpty(village.DisplayName) ? "Ninja" : village.DisplayName;
                string body = $"Castle Lv {village.CastleLevel}  ·  Best wave {village.HighestWave}\n" +
                              $"Chapters cleared: {village.ChaptersCleared}  ·  Trophies: {village.AchievementTiers}";
                var actions = UIBuilder.ActionCard(content, $"{name}'s Village", body, out _, out _, UITheme.Text, UIArt.Get("menu_village"));
                string uid = village.UserId;
                UIBuilder.SmallButton(actions.transform, "Visit", () => VillageVisitFlow.Visit(uid, name), UITheme.Button, 220f);
            }
            if (shown == 0)
                UIBuilder.Text(content, online ? "No neighbours yet — invite a friend!" : "", UITheme.BodySize, TextAlignmentOptions.Center, UITheme.TextMuted);
        }
    }
}
