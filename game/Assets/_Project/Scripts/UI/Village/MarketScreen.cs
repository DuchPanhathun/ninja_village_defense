using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Village;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Village
{
    /// <summary>
    /// The Market's daily shop (EPIC 11 "Daily shop"): today's rotating offers, each buyable once, with a
    /// countdown to the next restock. Stock is rolled deterministically per day, so reopening the screen
    /// (or the app) never re-rolls it.
    /// </summary>
    [SceneScreen(SceneNames.Village)]
    public class MarketScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Market;
        protected override string Title => "Market";

        private float _nextCountdownUpdate;

        protected override void Populate(RectTransform content)
        {
            if (!MarketService.IsOpen)
            {
                InfoText.text = "Build the Market in the village to unlock the daily shop.";
                return;
            }

            MarketService.EnsureStock();
            UpdateCountdown();

            int shown = 0;
            foreach (var state in MarketService.Stock)
            {
                if (state == null) continue;
                var offer = MarketService.GetOffer(state.OfferId);
                if (offer == null) continue;
                shown++;

                var actions = UIBuilder.ActionCard(content, offer.NameOrId, offer.DescribeReward(), out _, out _, null,
                    UIIcons.MarketOffer(offer), state.Purchased ? new Color(0.45f, 0.42f, 0.4f, 0.8f) : Color.white);
                if (state.Purchased)
                {
                    UIBuilder.Text(actions.transform, "Sold out", UITheme.SmallSize, TextAlignmentOptions.Right, UITheme.TextMuted);
                    continue;
                }

                var blocker = MarketService.Check(offer.Id);
                var buy = UIBuilder.SmallButton(actions.transform, $"Buy {offer.Price}", () =>
                {
                    if (MarketService.TryBuy(offer.Id, out var b, out string summary))
                    {
                        Sfx.Play(AudioCueIds.UiPurchase);
                        Toast(summary);
                    }
                    else
                    {
                        Sfx.Play(AudioCueIds.UiError);
                        Toast(MarketService.DescribeBlocker(b, offer));
                    }
                    Refresh();
                }, null, 300f);
                if (blocker != MarketBlocker.None) UIBuilder.SetEnabled(buy, false);
            }

            if (shown == 0)
                UIBuilder.Text(content, "No offers today. Run Ninja Village → Generate Default Content if this persists.", UITheme.BodySize);
        }

        private void Update()
        {
            if (!IsVisible || !MarketService.IsOpen || Time.unscaledTime < _nextCountdownUpdate) return;
            _nextCountdownUpdate = Time.unscaledTime + 1f;

            // Day rolled over while the shop was open → restock.
            if (MarketService.EnsureStock()) Refresh();
            else UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            InfoText.text = $"Market Lv {MarketService.MarketLevel} — {MarketService.OfferCount} offers per day. " +
                            $"New stock in {GameClock.FormatCountdown(GameClock.UntilNextDailyReset)}.";
        }
    }
}
