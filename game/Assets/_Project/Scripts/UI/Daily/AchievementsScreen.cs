using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Daily;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Daily
{
    /// <summary>
    /// Achievements (EPIC 19 "Achievement system"): every tiered achievement with current progress
    /// toward the next tier, tier stars and a claim button for each reached tier.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu)]
    public class AchievementsScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Achievements;
        protected override string Title => "Achievements";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterBadge() => ScreenBadges.Register(ScreenIds.Achievements, () => QuestService.HasClaimableAchievement);

        private void OnEnable() => EventBus<DailyContentChangedEvent>.Subscribe(OnDailyChanged);
        private void OnDisable() => EventBus<DailyContentChangedEvent>.Unsubscribe(OnDailyChanged);
        private void OnDailyChanged(DailyContentChangedEvent evt) { if (IsVisible) Refresh(); }

        protected override void Populate(RectTransform content)
        {
            var catalog = QuestService.Achievements;
            if (catalog == null || catalog.All.Count == 0)
            {
                InfoText.text = string.Empty;
                UIBuilder.Text(content, "No achievements found. Run Ninja Village → Generate Default Content.", UITheme.BodySize);
                return;
            }

            var list = new List<AchievementDefinition>();
            foreach (var a in catalog.All) if (a != null) list.Add(a);
            list.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));

            int totalTiers = 0, claimedTiers = 0;
            foreach (var achievement in list)
            {
                var progress = QuestService.AchievementProgress(achievement);
                totalTiers += achievement.TierCount;
                claimedTiers += progress.ClaimedTiers;
                AddCard(content, achievement, progress);
            }
            InfoText.text = $"{claimedTiers} / {totalTiers} tiers earned.";
        }

        private void AddCard(RectTransform content, AchievementDefinition achievement, NinjaVillage.Systems.Save.QuestProgress progress)
        {
            int reached = DailyRules.ReachedTiers(progress.Progress, achievement.TierTargets);
            int claimable = QuestService.ClaimableTiers(achievement);
            bool maxed = reached >= achievement.TierCount;
            int nextTarget = maxed ? achievement.TargetForTier(achievement.TierCount - 1) : achievement.TargetForTier(reached);

            string title = $"{achievement.DisplayName}  (tier {reached}/{achievement.TierCount})";
            string body = $"{achievement.Description}\n{Mathf.Min(progress.Progress, nextTarget)} / {nextTarget}" +
                          (maxed ? " — complete!" : $" · Next reward: {achievement.RewardForTier(reached)}");

            var actions = UIBuilder.ActionCard(content, title, body, out _, out _, maxed ? UITheme.Gold : UITheme.Text);
            if (claimable > 0)
            {
                UIBuilder.SmallButton(actions.transform, claimable > 1 ? $"Claim ({claimable})" : "Claim", () =>
                {
                    if (QuestService.TryClaimAchievementTier(achievement, out var reward)) Toast($"+{reward}");
                    Refresh();
                }, UITheme.Gold, 240f);
            }
            else if (maxed)
            {
                UIBuilder.Text(actions.transform, "Mastered", UITheme.SmallSize, TextAlignmentOptions.Right, UITheme.Gold);
            }
        }
    }
}
