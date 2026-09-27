using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Common
{
    /// <summary>
    /// "🪙 1.2K   💎 35" row that keeps itself up to date via <see cref="CurrencyChangedEvent"/> —
    /// every meta screen shows one so the player always sees what they can afford.
    /// </summary>
    public class CurrencyBar : MonoBehaviour
    {
        private TextMeshProUGUI _coins;
        private TextMeshProUGUI _gems;
        private bool _hasIcons;

        public static CurrencyBar Create(Transform parent, float height = 84f)
        {
            var row = UIBuilder.Horizontal(parent, "CurrencyBar", 16f, 0, TextAnchor.MiddleRight);
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            UIBuilder.SetPreferredSize(row, -1f, height);

            var bar = row.gameObject.AddComponent<CurrencyBar>();
            bar._coins = Pill(row.transform, "pickup_coin_0", UITheme.Gold, height, out bar._hasIcons);
            bar._gems = Pill(row.transform, "store_gems_100", UITheme.Gem, height, out _);
            bar.Refresh();
            return bar;
        }

        /// <summary>Dark wood pill with the currency icon and its amount (plain text when the art is missing).</summary>
        private static TextMeshProUGUI Pill(Transform row, string iconName, Color color, float height, out bool hasIcon)
        {
            var pill = UIBuilder.Image(row, "Pill", new Color(0.16f, 0.11f, 0.08f, 0.95f));
            UIBuilder.UseWood(pill, "panel_tint");
            pill.raycastTarget = false;
            UIBuilder.SetPreferredSize(pill, 250f, height);

            var sprite = UIArt.Get(iconName);
            hasIcon = sprite != null;
            if (hasIcon)
            {
                var icon = UIBuilder.Image(pill.transform, "Icon", Color.white, sprite);
                icon.raycastTarget = false;
                UIStyle.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(height - 8f, height - 8f));
            }
            var amount = UIStyle.Label(pill.transform, "", 34f, color, TextAlignmentOptions.Right, 0.2f);
            UIStyle.Place(amount.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(hasIcon ? 160f : 220f, height));
            return amount;
        }

        private void OnEnable()
        {
            EventBus<CurrencyChangedEvent>.Subscribe(OnCurrencyChanged);
            Refresh();
        }

        private void OnDisable() => EventBus<CurrencyChangedEvent>.Unsubscribe(OnCurrencyChanged);

        private void OnCurrencyChanged(CurrencyChangedEvent evt) => Refresh();

        public void Refresh()
        {
            if (_coins == null) return;
            var wallet = SaveService.Data.Wallet;
            string coins = UIBuilder.FormatAmount(wallet.Get(CurrencyType.Coins));
            string gems = UIBuilder.FormatAmount(wallet.Get(CurrencyType.Gems));
            _coins.text = _hasIcons ? coins : $"Coins {coins}";
            _gems.text = _hasIcons ? gems : $"Gems {gems}";
        }
    }
}
