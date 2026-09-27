using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Daily;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Save;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Daily
{
    /// <summary>
    /// Daily and weekly quests (EPIC 19): today's and this week's rolled quests with progress bars,
    /// claim buttons for completed ones, and countdowns to the next reset.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu)]
    public class QuestsScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Quests;
        protected override string Title => "Quests";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterBadge() => ScreenBadges.Register(ScreenIds.Quests, () => QuestService.HasClaimableQuest);

        private void OnEnable() => EventBus<DailyContentChangedEvent>.Subscribe(OnDailyChanged);
        private void OnDisable() => EventBus<DailyContentChangedEvent>.Unsubscribe(OnDailyChanged);
        private void OnDailyChanged(DailyContentChangedEvent evt) { if (IsVisible) Refresh(); }

        protected override void Populate(RectTransform content)
        {
            QuestService.EnsureRolled();
            InfoText.text = "Complete quests in battle and around the village to earn rewards.";

            UIBuilder.SectionHeader(content, $"Daily — resets in {GameClock.FormatCountdown(GameClock.UntilNextDailyReset)}");
            AddQuests(content, QuestService.Daily);

            UIBuilder.SectionHeader(content, $"Weekly — resets in {GameClock.FormatCountdown(GameClock.UntilNextWeeklyReset)}");
            AddQuests(content, QuestService.Weekly);
        }

        private void AddQuests(RectTransform content, IReadOnlyList<QuestProgress> quests)
        {
            if (quests.Count == 0)
            {
                UIBuilder.Text(content, "No quests available. Run Ninja Village → Generate Default Content.", UITheme.BodySize, TextAlignmentOptions.Left, UITheme.TextMuted);
                return;
            }

            foreach (var progress in quests)
            {
                var quest = QuestService.GetQuest(progress.Id);
                if (quest == null) continue;

                string body = $"{Mathf.Min(progress.Progress, quest.Target)} / {quest.Target} · Reward: {quest.Reward}";
                Color accent = progress.Claimed ? UITheme.TextMuted : progress.Completed ? UITheme.Gold : UITheme.Text;
                var actions = UIBuilder.ActionCard(content, quest.TaskText, body, out _, out var bodyText, accent);

                UIBuilder.ProgressBar(bodyText.transform.parent, "Progress", out var fill, progress.Completed ? UITheme.Gold : UITheme.Positive);
                fill.fillAmount = Mathf.Clamp01(progress.Progress / (float)quest.Target);
                ((RectTransform)fill.transform.parent).SetSiblingIndex(bodyText.transform.GetSiblingIndex() + 1);

                if (progress.Claimed)
                    UIBuilder.Text(actions.transform, "Claimed", UITheme.SmallSize, TextAlignmentOptions.Right, UITheme.Positive);
                else if (progress.Completed)
                    UIBuilder.SmallButton(actions.transform, "Claim", () =>
                    {
                        if (QuestService.TryClaimQuest(progress)) Toast($"+{quest.Reward}");
                        Refresh();
                    }, UITheme.Gold, 200f);
            }
        }
    }
}
