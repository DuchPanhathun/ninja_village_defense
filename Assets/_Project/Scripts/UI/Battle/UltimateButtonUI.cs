using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Ultimates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Battle
{
    /// <summary>
    /// The big ultimate button: fills up as charge builds; when fully charged but the ultimate is still on
    /// its cooldown it shows the time left instead, and only says READY! (and accepts taps) when a tap will
    /// really fire it — so it never looks ready while ignoring the player.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UltimateButtonUI : MonoBehaviour
    {
        [SerializeField] private Image chargeFillImage;
        [Tooltip("When on, chargeFillImage is a dark cover showing the part still to charge (it recedes as charge builds).")]
        [SerializeField] private bool fillShowsRemaining;
        [Tooltip("Optional: shows the equipped ultimate's icon.")]
        [SerializeField] private Image iconImage;
        [Tooltip("Optional: charge percentage, then READY!")]
        [SerializeField] private TMP_Text label;

        private Button _button;
        private UltimateController _ultimate;
        private UltimateDefinition _shownUltimate;
        private float _charge;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.interactable = false;
            _button.onClick.AddListener(OnPressed);
        }

        private void OnEnable() => EventBus<UltimateChargeChangedEvent>.Subscribe(OnChargeChanged);
        private void OnDisable() => EventBus<UltimateChargeChangedEvent>.Unsubscribe(OnChargeChanged);

        private void OnChargeChanged(UltimateChargeChangedEvent evt)
        {
            _charge = Mathf.Clamp01(evt.Normalized);
            RefreshState();
        }

        /// <summary>Charge %, then the cooldown countdown if one is still running, then READY!.</summary>
        private void RefreshState()
        {
            if (_ultimate != null) _charge = _ultimate.ChargeNormalized;
            float cooldown = _ultimate != null ? _ultimate.CooldownRemaining : 0f;
            bool charged = _charge >= 1f;
            bool ready = _ultimate != null ? _ultimate.IsReady : charged;

            // The cover shows what's left: charge still to build, or (charged) the cooldown still to run.
            float remaining = !charged ? 1f - _charge : ready ? 0f : _ultimate.CooldownNormalized;
            if (chargeFillImage != null)
                chargeFillImage.fillAmount = fillShowsRemaining ? remaining : 1f - remaining;
            _button.interactable = ready;
            if (label != null)
            {
                int seconds = Mathf.CeilToInt(cooldown);
                label.text = ready ? "READY!" : charged ? $"{seconds / 60}:{seconds % 60:00}" : $"{Mathf.FloorToInt(_charge * 100f)}%";
            }
        }

        private void Update()
        {
            if (_ultimate == null && PlayerReference.Instance != null)
                _ultimate = PlayerReference.Instance.GetComponent<UltimateController>();

            // Icon follows the equipped ultimate (hero/run-start modifiers can change it after Start).
            var equipped = _ultimate != null ? _ultimate.EquippedUltimate : null;
            if (iconImage != null && equipped != _shownUltimate)
            {
                _shownUltimate = equipped;
                iconImage.sprite = equipped != null ? equipped.Icon : null;
                iconImage.enabled = iconImage.sprite != null;
            }

            RefreshState(); // the cooldown runs on its own time, not on charge events

            // Ready: a gentle pulse so it catches the eye.
            float scale = _button.interactable ? 1f + 0.07f * Mathf.Sin(Time.unscaledTime * 7f) : 1f;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void OnPressed()
        {
            if (_ultimate == null && PlayerReference.Instance != null)
                _ultimate = PlayerReference.Instance.GetComponent<UltimateController>();

            _ultimate?.TryActivate();
        }
    }
}
