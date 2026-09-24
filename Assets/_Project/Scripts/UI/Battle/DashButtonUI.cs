using NinjaVillage.Gameplay.Player;
using NinjaVillage.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Battle
{
    /// <summary>
    /// On-screen dash button for touch play, with a radial cooldown fill. Built in code on its own
    /// overlay canvas whenever a <see cref="DashController"/> starts, so no scene wiring is needed.
    /// Sits on the right edge above the ultimate button, clear of the joystick's thumb area.
    /// </summary>
    public class DashButtonUI : MonoBehaviour
    {
        private DashController _dash;
        private Image _cooldownFill;
        private Button _button;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DashController.Spawned -= Create;
            DashController.Spawned += Create;
        }

        private static void Create(DashController dash)
        {
            var canvas = UIBuilder.CreateCanvas("[Battle Controls]", 5);
            var ui = canvas.gameObject.AddComponent<DashButtonUI>();
            ui._dash = dash;

            // Wood button from the pixel-art UI kit (flat blue when the art catalog is missing).
            var image = UIStyle.Sprite(canvas.transform, "DashButton", "panel_wood_panel", new Color(0.25f, 0.55f, 0.95f, 0.85f));
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(190f, 190f);
            rt.anchoredPosition = new Vector2(-40f, 520f);

            ui._button = image.gameObject.AddComponent<Button>();
            ui._button.targetGraphic = image;
            ui._button.onClick.AddListener(() => ui._dash.TryDash());

            var label = UIStyle.Label(rt, "DASH", UITheme.HeaderSize, Color.white);
            UIBuilder.Stretch(label.rectTransform);

            ui._cooldownFill = UIBuilder.Image(rt, "Cooldown", new Color(0f, 0f, 0f, 0.55f), UIBuilder.WhiteSprite);
            UIBuilder.Stretch(ui._cooldownFill.rectTransform);
            ui._cooldownFill.type = Image.Type.Filled;
            ui._cooldownFill.fillMethod = Image.FillMethod.Radial360;
            ui._cooldownFill.fillOrigin = (int)Image.Origin360.Top;
            ui._cooldownFill.raycastTarget = false;
        }

        private void Update()
        {
            if (_dash == null)
            {
                Destroy(gameObject);
                return;
            }
            _cooldownFill.fillAmount = _dash.CooldownNormalized;
            _button.interactable = _dash.IsReady;
        }
    }
}
