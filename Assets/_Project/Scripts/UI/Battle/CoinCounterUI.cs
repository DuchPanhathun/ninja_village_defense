using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Economy;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Battle
{
    /// <summary>HUD coin counter, updated by CurrencyChangedEvent.</summary>
    public class CoinCounterUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text coinText;

        private void Start()
        {
            if (EconomyManager.Instance != null)
                SetAmount(EconomyManager.Instance.Wallet.Coins);
        }

        private void OnEnable() => EventBus<CurrencyChangedEvent>.Subscribe(OnCurrencyChanged);
        private void OnDisable() => EventBus<CurrencyChangedEvent>.Unsubscribe(OnCurrencyChanged);

        private void OnCurrencyChanged(CurrencyChangedEvent evt)
        {
            if (evt.Type == CurrencyType.Coins)
                SetAmount(evt.NewBalance);
        }

        private void SetAmount(int amount)
        {
            if (coinText != null)
                coinText.text = amount.ToString("N0");
        }
    }
}
