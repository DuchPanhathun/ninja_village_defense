using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.LiveOps;
using NinjaVillage.Systems.Save;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.LiveOps
{
    /// <summary>
    /// Seasonal event hub (EPIC 20): the active event with its countdown and battle bonuses, its missions
    /// (progress + claim), and the limited-time shop. With no event running it shows when the next starts.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu)]
    public class EventsScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Events;
        protected override string Title => "Events";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterBadge() => ScreenBadges.Register(ScreenIds.Events, HasClaimable);

        private static bool HasClaimable()
        {
            if (LiveOpsService.EnsureEvent() == null) return false;
            foreach (var p in LiveOpsService.MissionProgress)
                if (p != null && p.Completed && !p.Claimed) return true;
            return false;
        }

        private void OnEnable() => EventBus<LiveOpsChangedEvent>.Subscribe(OnChanged);
        private void OnDisable() => EventBus<LiveOpsChangedEvent>.Unsubscribe(OnChanged);
        private void OnChanged(LiveOpsChangedEvent evt) { if (IsVisible) Refresh(); }

        protected override void Populate(RectTransform content)
        {
            var active = LiveOpsService.EnsureEvent();
            if (active == null)
            {
                var next = LiveOpsService.NextEvent;
                InfoText.text = next != null && next.StartUtc.HasValue
                    ? $"No event right now. <b>{next.DisplayName}</b> starts in {GameClock.FormatCountdown(next.StartUtc.Value - GameClock.UtcNow)}."
                    : "No events scheduled right now — check back soon!";
            }
            else
            {
                string bonuses = string.Empty;
                if (active.CoinBonus > 0f) bonuses += $" +{active.CoinBonus * 100f:0}% coins";
                if (active.XpBonus > 0f) bonuses += $" +{active.XpBonus * 100f:0}% XP";
                InfoText.text = $"<b>{active.DisplayName}</b> — ends in {GameClock.FormatCountdown(active.EndUtc.Value - GameClock.UtcNow)}" +
                                (bonuses.Length > 0 ? $"\nBattle bonus:{bonuses}" : string.Empty) +
                                (string.IsNullOrEmpty(active.Description) ? string.Empty : "\n" + active.Description);

                UIBuilder.SectionHeader(content, "Event missions");
                foreach (var progress in LiveOpsService.MissionProgress)
                {
                    var mission = active.GetMission(progress.Id);
                    if (mission == null) continue;
                    string body = $"{Mathf.Min(progress.Progress, mission.target)} / {mission.target} · Reward: {LiveOpsRewards.Describe(mission.rewards)}";
                    var actions = UIBuilder.ActionCard(content, mission.description, body, out _, out _,
                        progress.Claimed ? UITheme.TextMuted : progress.Completed ? UITheme.Gold : active.ThemeColor);
                    if (progress.Claimed)
                        UIBuilder.Text(actions.transform, "Claimed", UITheme.SmallSize, TextAlignmentOptions.Right, UITheme.Positive);
                    else if (progress.Completed)
                        UIBuilder.SmallButton(actions.transform, "Claim", () =>
                        {
                            if (LiveOpsService.TryClaimMission(mission.id)) Toast("Reward claimed!");
                            Refresh();
                        }, UITheme.Gold, 200f);
                }
            }

            var offers = LiveOpsService.AvailableOffers();
            if (offers.Count == 0) return;
            UIBuilder.SectionHeader(content, "Limited-time shop");
            foreach (var offer in offers) AddOffer(content, offer);
        }

        private void AddOffer(RectTransform content, LimitedOfferDefinition offer)
        {
            int bought = LiveOpsService.Purchases(offer);
            var ends = LiveOpsService.OfferEndsUtc(offer);
            string limit = offer.PurchaseLimit > 0 ? $"{bought}/{offer.PurchaseLimit} bought" : "Unlimited";
            string body = $"{LiveOpsRewards.Describe(offer.Rewards)}\n{limit}" +
                          (ends.HasValue ? $" · {GameClock.FormatCountdown(ends.Value - GameClock.UtcNow)} left" : string.Empty);
            var actions = UIBuilder.ActionCard(content, $"[{offer.Badge}] {offer.DisplayName}", body, out _, out _, UITheme.Gem);

            var check = LiveOpsService.CheckOffer(offer);
            if (check == LiveOpsService.OfferResult.SoldOut)
            {
                UIBuilder.Text(actions.transform, "Sold out", UITheme.SmallSize, TextAlignmentOptions.Right, UITheme.TextMuted);
                return;
            }
            var buy = UIBuilder.SmallButton(actions.transform, $"Buy {offer.Price}", () =>
            {
                var result = LiveOpsService.TryBuyOffer(offer);
                if (result == LiveOpsService.OfferResult.Ok)
                {
                    Sfx.Play(AudioCueIds.UiPurchase);
                    Toast("Purchased!");
                }
                else
                {
                    Sfx.Play(AudioCueIds.UiError);
                    Toast(result == LiveOpsService.OfferResult.NotEnoughCurrency ? "Not enough currency" : "Unavailable");
                }
                Refresh();
            }, UITheme.Button, 300f);
            if (check != LiveOpsService.OfferResult.Ok) UIBuilder.SetEnabled(buy, false);
        }
    }
}
