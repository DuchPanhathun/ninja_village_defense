using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.LiveOps;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.LiveOps
{
    /// <summary>
    /// Battle pass (EPIC 20): season name and countdown, XP bar toward the next tier, and the tier track
    /// with a free and a premium reward per tier. Premium tiers show a lock until the pass is bought in
    /// the Shop; buying later lets every reached premium tier be claimed retroactively.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu)]
    public class BattlePassScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.BattlePass;
        protected override string Title => "Battle Pass";

        private Image _xpFill;
        private Button _premiumButton;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterBadge() => ScreenBadges.Register(ScreenIds.BattlePass, () => BattlePassService.ClaimableCount > 0);

        protected override void BuildTop(RectTransform body)
        {
            UIBuilder.ProgressBar(body, "SeasonXp", out _xpFill, UITheme.Gold, 34f);
            _premiumButton = UIBuilder.Button(body, "Unlock Premium Pass", () =>
            {
                Sfx.Play(AudioCueIds.UiClick);
                if (UIScreenNavigator.Instance.Has(ScreenIds.Store)) UIScreenNavigator.Instance.Show(ScreenIds.Store);
                else Toast("The Shop isn't available here");
            }, UITheme.Gold, 110f);
        }

        private void OnEnable() => EventBus<BattlePassChangedEvent>.Subscribe(OnChanged);
        private void OnDisable() => EventBus<BattlePassChangedEvent>.Unsubscribe(OnChanged);
        private void OnChanged(BattlePassChangedEvent evt) { if (IsVisible) Refresh(); }

        protected override void Populate(RectTransform content)
        {
            var season = BattlePassService.EnsureSeason();
            if (season == null)
            {
                var next = BattlePassService.NextSeason;
                InfoText.text = next != null && next.StartUtc.HasValue
                    ? $"The next season, <b>{next.DisplayName}</b>, starts in {GameClock.FormatCountdown(next.StartUtc.Value - GameClock.UtcNow)}."
                    : "No battle pass season is running right now.";
                _xpFill.fillAmount = 0f;
                _premiumButton.gameObject.SetActive(false);
                return;
            }

            int reached = BattlePassService.TiersReached;
            int xpInTier = season.XpPerTier > 0 ? BattlePassService.Xp % season.XpPerTier : 0;
            InfoText.text = $"<b>{season.DisplayName}</b> · Tier {reached}/{season.TierCount}" +
                            (reached < season.TierCount ? $" · {xpInTier}/{season.XpPerTier} XP" : " · Complete!") +
                            $"\nEnds in {GameClock.FormatCountdown(BattlePassService.TimeLeft)}. Earn XP from battles, bosses, quests and logins.";
            _xpFill.fillAmount = LiveOpsRules.TierProgress(BattlePassService.Xp, season.XpPerTier, season.TierCount);
            _premiumButton.gameObject.SetActive(!BattlePassService.IsPremiumUnlocked);

            for (int tier = 0; tier < season.TierCount; tier++)
                AddTier(content, season, tier, tier < reached);
        }

        private void AddTier(RectTransform content, SeasonDefinition season, int tier, bool reached)
        {
            var free = season.FreeReward(tier);
            var premium = season.PremiumReward(tier);
            string body = $"Free: {(season.HasFreeReward(tier) ? free.ToString() : "—")}\nPremium: {premium}";
            var actions = UIBuilder.ActionCard(content, $"Tier {tier + 1}", body, out _, out _, reached ? UITheme.Gold : UITheme.TextMuted);

            if (!reached)
            {
                UIBuilder.Text(actions.transform, "Locked", UITheme.SmallSize, TextAlignmentOptions.Right, UITheme.TextMuted);
                return;
            }

            if (season.HasFreeReward(tier)) AddClaim(actions.transform, tier, premium: false);
            AddClaim(actions.transform, tier, premium: true);
        }

        private void AddClaim(Transform parent, int tier, bool premium)
        {
            if (BattlePassService.IsClaimed(tier, premium))
            {
                UIBuilder.Text(parent, premium ? "Premium claimed" : "Free claimed", UITheme.SmallSize, TextAlignmentOptions.Right, UITheme.Positive);
                return;
            }
            var check = BattlePassService.CheckClaim(tier, premium);
            string label = premium ? (check == LiveOpsRules.ClaimResult.PremiumLocked ? "Premium (locked)" : "Claim Premium") : "Claim";
            var button = UIBuilder.SmallButton(parent, label, () =>
            {
                if (!BattlePassService.TryClaim(tier, premium)) Sfx.Play(AudioCueIds.UiError);
                Refresh();
            }, premium ? UITheme.Gold : (Color?)null, premium ? 260f : 170f);
            if (check != LiveOpsRules.ClaimResult.Ok) UIBuilder.SetEnabled(button, false);
        }
    }
}
