using NinjaVillage.Gameplay.Player;
using NinjaVillage.UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Battle
{
    /// <summary>
    /// On-screen JUMP button (hop over trees, rocks and enemies), with a radial cooldown fill. Built in code
    /// on its own overlay canvas whenever a <see cref="JumpController"/> starts; sits left of the Dash
    /// button so both are under the right thumb, clear of the joystick.
    /// </summary>
    public class JumpButtonUI : MonoBehaviour
    {
        private JumpController _jump;
        private Image _cooldownFill;
        private Button _button;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            JumpController.Spawned -= Create;
            JumpController.Spawned += Create;
        }

        private static void Create(JumpController jump)
        {
            var canvas = UIBuilder.CreateCanvas("[Jump Control]", 5);
            var ui = canvas.gameObject.AddComponent<JumpButtonUI>();
            ui._jump = jump;

            var image = UIStyle.Sprite(canvas.transform, "JumpButton", "panel_wood_panel", new Color(0.35f, 0.75f, 0.4f, 0.85f));
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(170f, 170f);
            rt.anchoredPosition = new Vector2(-250f, 400f);

            ui._button = image.gameObject.AddComponent<Button>();
            ui._button.targetGraphic = image;
            ui._button.onClick.AddListener(() => ui._jump.TryJump());

            var label = UIStyle.Label(rt, "JUMP", UITheme.HeaderSize * 0.9f, Color.white);
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
            if (_jump == null)
            {
                Destroy(gameObject);
                return;
            }
            // Above the HUD, so hide while paused (pause menu, level-up cards, revive prompt).
            bool paused = Time.timeScale <= 0f;
            if (_button.gameObject.activeSelf == paused) _button.gameObject.SetActive(!paused);
            if (paused) return;
            _cooldownFill.fillAmount = _jump.IsJumping ? 1f : _jump.CooldownNormalized;
            _button.interactable = _jump.IsReady;
        }
    }
}
