using System.Collections.Generic;
using NinjaVillage.Systems.Backend;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Social;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Village
{
    /// <summary>
    /// Neighbours: this week's Best Villages (most liked — the top three win a trophy decoration) and the most
    /// recently active villages of other players (from Firebase), each with its owner's castle, best wave,
    /// chapters and trophies, and a Visit button that opens that village (read-only, plus Like / Gift / Water).
    /// </summary>
    [SceneScreen(SceneNames.Village, SceneNames.MainMenu)]
    public class NeighboursScreen : UIListScreen
    {
        public const int Count = 30;

        public override string ScreenId => ScreenIds.Neighbours;
        protected override string Title => "Neighbours";

        public const int BestCount = 10;

        private List<PublicVillage> _villages, _best;
        private bool _loading;

        protected override async void OnShown()
        {
            base.OnShown();
            if (_loading) return;
            _loading = true;
            _villages = null;
            _best = null;
            Refresh();
            _best = await BackendService.ListTopVillagesAsync(BestCount);
            _villages = await BackendService.ListVillagesAsync(Count + 1);
            _loading = false;
            if (this != null && IsVisible) Refresh();
        }

        /// <summary>This week's most liked villages, with their place; the top three win a trophy when the week ends.</summary>
        private void PopulateBest(RectTransform content, string myId)
        {
            UIBuilder.SectionHeader(content, "Best villages this week");
            if (_best == null || _best.Count == 0)
            {
                UIBuilder.Text(content, $"No likes yet this week. The {SocialRules.TrophyPlaces} most liked villages win a golden trophy when the week ends!",
                    UITheme.SmallSize, TextAlignmentOptions.Center, UITheme.TextMuted);
                return;
            }
            for (int i = 0; i < _best.Count; i++)
            {
                var village = _best[i];
                if (village == null) continue;
                string name = string.IsNullOrEmpty(village.DisplayName) ? "Ninja" : village.DisplayName;
                bool mine = village.UserId == myId;
                bool trophy = i < SocialRules.TrophyPlaces;
                string body = $"<color=#FF9AAE>{village.Likes} like{(village.Likes == 1 ? "" : "s")}</color>  ·  Castle Lv {village.CastleLevel}" +
                              (trophy ? "\n<color=#FFD24D>Trophy place</color>" : "");
                var actions = UIBuilder.ActionCard(content, $"#{i + 1}  {(mine ? "Your village" : name + "'s Village")}", body,
                    out _, out _, trophy ? UITheme.Gold : UITheme.Text, UIArt.Get(trophy ? "menu_achievements" : "menu_village"));
                if (mine) continue;
                string uid = village.UserId;
                UIBuilder.SmallButton(actions.transform, "Visit", () => VillageVisitFlow.Visit(uid, name), UITheme.Button, 220f);
            }
        }

        protected override void Populate(RectTransform content)
        {
            var provider = BackendService.Provider;
            bool online = provider != null && provider.IsOnline;
            int likes = SocialService.LikesThisWeek;
            InfoText.text = (online
                ? "Visit a village to like it, leave a gift or water its crops. "
                : "Offline — neighbours appear on phone builds with an internet connection. ") +
                $"Your village: <color=#FF9AAE>{likes} like{(likes == 1 ? "" : "s")}</color> this week.";

            if (_villages == null)
            {
                UIBuilder.Text(content, "Loading...", UITheme.BodySize, TextAlignmentOptions.Center, UITheme.TextMuted);
                return;
            }

            string myId = provider?.CurrentUser?.UserId;
            PopulateBest(content, myId);
            UIBuilder.SectionHeader(content, "Played recently");
            int shown = 0;
            foreach (var village in _villages)
            {
                if (village == null || village.UserId == myId || shown >= Count) continue;
                shown++;
                string name = string.IsNullOrEmpty(village.DisplayName) ? "Ninja" : village.DisplayName;
                string body = $"Castle Lv {village.CastleLevel}  ·  Best wave {village.HighestWave}\n" +
                              $"Chapters cleared: {village.ChaptersCleared}  ·  Trophies: {village.AchievementTiers}";
                if (village.LikesWeek == NinjaVillage.Core.Utilities.GameClock.ThisWeek && village.Likes > 0)
                    body += $"  ·  <color=#FF9AAE>{village.Likes} like{(village.Likes == 1 ? "" : "s")}</color>";
                var actions = UIBuilder.ActionCard(content, $"{name}'s Village", body, out _, out _, UITheme.Text, UIArt.Get("menu_village"));
                string uid = village.UserId;
                UIBuilder.SmallButton(actions.transform, "Visit", () => VillageVisitFlow.Visit(uid, name), UITheme.Button, 220f);
            }
            if (shown == 0)
                UIBuilder.Text(content, online ? "No neighbours yet — invite a friend!" : "", UITheme.BodySize, TextAlignmentOptions.Center, UITheme.TextMuted);
        }
    }
}
