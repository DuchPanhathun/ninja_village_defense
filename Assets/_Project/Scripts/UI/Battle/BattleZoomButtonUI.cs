using NinjaVillage.Core.Audio;
using NinjaVillage.Gameplay.Camera;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Settings;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NinjaVillage.UI.Battle
{
    /// <summary>
    /// The battle's VIEW button, under the minimap: each tap widens the camera one step (Normal → Wide → Widest → back
    /// to Normal) so you can see more of the battlefield, and remembers the choice for the next run (also in
    /// Settings). Built in code, with <see cref="BattleCameraZoom"/> on the camera, whenever the Battle scene loads.
    /// </summary>
    public class BattleZoomButtonUI : MonoBehaviour
    {
        private Button _button;
        private TextMeshProUGUI _label;

        public Button Button => _button;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneNames.Battle) return;
            if (BattleCameraZoom.Ensure(UnityEngine.Camera.main) == null) return;
            Create();
        }

        private static void Create()
        {
            var canvas = UIBuilder.CreateCanvas("[Zoom Control]", 5);
            var ui = canvas.gameObject.AddComponent<BattleZoomButtonUI>();

            var frame = UIStyle.Sprite(canvas.transform, "ZoomButton", "panel_wood_panel", new Color(0.35f, 0.55f, 0.85f, 0.9f));
            var rt = frame.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(124f, 124f);
            rt.anchoredPosition = new Vector2(-54f, -470f);

            ui._button = frame.gameObject.AddComponent<Button>();
            ui._button.targetGraphic = frame;
            ui._button.onClick.AddListener(ui.Cycle);

            var eye = UIBuilder.Image(rt, "Icon", Color.white, UIArt.Get("icon_talent_keen_eye"));
            eye.raycastTarget = false;
            eye.preserveAspect = true;
            UIBuilder.Stretch(eye.rectTransform, 16f);

            ui._label = UIStyle.Label(rt, "", 26f, Color.white, TextAlignmentOptions.Center, 0.3f);
            UIStyle.Place(ui._label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(160f, 36f));
            ui.RefreshLabel();
        }

        /// <summary>One step wider (Widest wraps back to Normal). Public so tests can drive it like a tap.</summary>
        public void Cycle()
        {
            Sfx.Play(AudioCueIds.UiClick);
            SettingsService.SetBattleZoom(SettingsSaveData.NextBattleZoom(SettingsService.Current.BattleZoom));
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            int level = SettingsService.Current.BattleZoom;
            _label.text = $"{SettingsSaveData.BattleZoomNames[SettingsSaveData.ClampBattleZoom(level)]}";
        }

        private void Update()
        {
            // Above the HUD, so hide while paused (pause menu, level-up cards, revive prompt).
            bool paused = Time.timeScale <= 0f;
            if (_button.gameObject.activeSelf == paused) _button.gameObject.SetActive(!paused);
        }
    }
}
