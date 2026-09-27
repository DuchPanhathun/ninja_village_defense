using NinjaVillage.Core.Combat;
using NinjaVillage.Gameplay.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Battle
{
    /// <summary>
    /// HUD health bar. Finds the player via PlayerReference and polls — polling a
    /// single fill image per frame is cheaper than event bookkeeping and never
    /// misses heals/shields.
    /// </summary>
    public class PlayerHealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Image shieldFillImage;
        [SerializeField] private TMP_Text valueText;

        private Health _health;

        private void Update()
        {
            if (_health == null)
            {
                if (PlayerReference.Instance == null) return;
                _health = PlayerReference.Instance.GetComponent<Health>();
                if (_health == null) return;
            }

            float normalized = _health.MaxHealth > 0f ? _health.CurrentHealth / _health.MaxHealth : 0f;
            if (fillImage != null)
                fillImage.fillAmount = normalized;

            if (shieldFillImage != null)
                shieldFillImage.fillAmount = _health.MaxHealth > 0f ? Mathf.Clamp01(_health.CurrentShield / _health.MaxHealth) : 0f;

            if (valueText != null)
                valueText.text = $"{Mathf.CeilToInt(_health.CurrentHealth)}/{Mathf.CeilToInt(_health.MaxHealth)}";
        }
    }
}
