using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Ultimates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Battle
{
    /// <summary>
    /// The big ultimate button: fills up as charge builds, becomes interactable at
    /// full charge, fires the equipped ultimate on tap.
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
            if (chargeFillImage != null)
                chargeFillImage.fillAmount = fillShowsRemaining ? 1f - _charge : _charge;
            _button.interactable = _charge >= 1f;
            if (label != null) label.text = _charge >= 1f ? "READY!" : $"{Mathf.FloorToInt(_charge * 100f)}%";
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

            // Ready: a gentle pulse so it catches the eye.
            float scale = _charge >= 1f ? 1f + 0.07f * Mathf.Sin(Time.unscaledTime * 7f) : 1f;
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
