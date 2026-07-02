using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Ultimates;
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

        private Button _button;
        private UltimateController _ultimate;

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
            if (chargeFillImage != null)
                chargeFillImage.fillAmount = evt.Normalized;
            _button.interactable = evt.Normalized >= 1f;
        }

        private void OnPressed()
        {
            if (_ultimate == null && PlayerReference.Instance != null)
                _ultimate = PlayerReference.Instance.GetComponent<UltimateController>();

            _ultimate?.TryActivate();
        }
    }
}
