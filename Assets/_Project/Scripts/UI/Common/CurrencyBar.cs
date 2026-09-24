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

        public static CurrencyBar Create(Transform parent, float height = 70f)
        {
            var row = UIBuilder.Horizontal(parent, "CurrencyBar", 24f, 8, TextAnchor.MiddleRight);
            row.childForceExpandWidth = false;
            UIBuilder.SetPreferredSize(row, -1f, height);

            var bar = row.gameObject.AddComponent<CurrencyBar>();
            bar._coins = UIBuilder.Text(row.transform, "", UITheme.BodySize, TextAlignmentOptions.Right, UITheme.Gold, FontStyles.Bold);
            bar._gems = UIBuilder.Text(row.transform, "", UITheme.BodySize, TextAlignmentOptions.Right, UITheme.Gem, FontStyles.Bold);
            UIBuilder.SetPreferredSize(bar._coins, 260f, -1f);
            UIBuilder.SetPreferredSize(bar._gems, 200f, -1f);
            bar.Refresh();
            return bar;
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
            _coins.text = $"Coins {UIBuilder.FormatAmount(wallet.Get(CurrencyType.Coins))}";
            _gems.text = $"Gems {UIBuilder.FormatAmount(wallet.Get(CurrencyType.Gems))}";
        }
    }
}
