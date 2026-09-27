using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Daily;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Daily
{
    /// <summary>
    /// The 7-day login calendar (EPIC 19 "Daily login"): claimed days, today's reward with a claim
    /// button, upcoming days, the current streak and time until tomorrow's reward.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu)]
    public class DailyLoginScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.DailyLogin;
        protected override string Title => "Daily Reward";

        private Button _claim;
        private float _nextTick;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterBadge() => ScreenBadges.Register(ScreenIds.DailyLogin, () => DailyLoginService.CanClaimToday);

        protected override void BuildBottom(RectTransform body)
        {
            _claim = UIBuilder.Button(body, "Claim", OnClaim, UITheme.Button, 140f, UITheme.HeaderSize);
        }

        protected override void Populate(RectTransform content)
        {
            bool canClaim = DailyLoginService.CanClaimToday;
            int next = DailyLoginService.NextDayIndex;
            int length = DailyLoginService.CalendarLength;

            UpdateInfo();
            UIBuilder.SetEnabled(_claim, canClaim);
            UIBuilder.SetLabel(_claim, canClaim ? $"Claim Day {next + 1}: {DailyLoginService.RewardAt(next)}" : "Come back tomorrow!");

            for (int day = 0; day < length; day++)
            {
                var reward = DailyLoginService.RewardAt(day);
                bool claimed = day < DailyLoginService.ClaimedThisCycle;
                bool today = canClaim && day == next;
                string state = today ? "<color=#FFCC40>Today!</color>" : claimed ? "<color=#66D973>Claimed</color>" : "Upcoming";
                Color accent = today ? UITheme.Gold : claimed ? UITheme.Positive : UITheme.Text;
                UIBuilder.ActionCard(content, $"Day {day + 1} — {reward}", state, out _, out _, accent);
            }
        }

        private void UpdateInfo()
        {
            InfoText.text = $"Login streak: {DailyLoginService.Streak} day(s) · Best: {DailyLoginService.BestStreak}\n" +
                            (DailyLoginService.CanClaimToday
                                ? "Your reward is ready!"
                                : $"Next reward in {GameClock.FormatCountdown(GameClock.UntilNextDailyReset)}");
        }

        private void OnClaim()
        {
            if (DailyLoginService.TryClaim(out var reward))
                Toast($"+{reward}  (streak {DailyLoginService.Streak})");
            else
                Sfx.Play(AudioCueIds.UiError);
            Refresh();
        }

        private void Update()
        {
            if (!IsVisible || Time.unscaledTime < _nextTick) return;
            _nextTick = Time.unscaledTime + 1f;
            if (DailyLoginService.CanClaimToday && !_claim.interactable) Refresh(); // day rolled over
            else UpdateInfo();
        }
    }
}
