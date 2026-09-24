using System.Collections;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Audio;
using NinjaVillage.Systems.Evolution;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Battle
{
    /// <summary>
    /// Big celebratory banner over the battle (EPIC 5 "Level animation", EPIC 7 "Unlock animation"):
    /// a screen flash, then a title that punches in, holds and fades. Runs on unscaled time because
    /// level-ups immediately pause the game for the skill choice. Built in code on its own top canvas
    /// the first time it's needed in a scene; listens persistently so no scene wiring is required.
    /// </summary>
    public class BattleBanner : MonoBehaviour
    {
        private static BattleBanner _instance;

        private TextMeshProUGUI _title;
        private TextMeshProUGUI _subtitle;
        private Image _flash;
        private CanvasGroup _group;
        private Coroutine _routine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            EventBus<LevelUpEvent>.UnsubscribePersistent(OnLevelUp);
            EventBus<EvolutionUnlockedEvent>.UnsubscribePersistent(OnEvolution);
            EventBus<LevelUpEvent>.SubscribePersistent(OnLevelUp);
            EventBus<EvolutionUnlockedEvent>.SubscribePersistent(OnEvolution);
        }

        private static void OnLevelUp(LevelUpEvent evt) =>
            Show("LEVEL UP!", $"Level {evt.NewLevel}", UITheme.Gold, 1.1f, 0.25f);

        private static void OnEvolution(EvolutionUnlockedEvent evt)
        {
            var recipe = evt.Recipe;
            string result = recipe != null && recipe.ResultSkill != null ? recipe.ResultSkill.DisplayName : "a forbidden technique";
            Show("EVOLUTION!", $"You discovered {result}", new Color(0.75f, 0.45f, 1f), 2.4f, 0.55f);
            Sfx.Play(AudioCueIds.EvolutionUnlock);
        }

        /// <summary>Shows a banner, replacing any banner already on screen.</summary>
        public static void Show(string title, string subtitle, Color color, float holdSeconds, float flashAlpha)
        {
            if (_instance == null) _instance = Create();
            _instance.Play(title, subtitle, color, holdSeconds, flashAlpha);
        }

        private static BattleBanner Create()
        {
            var canvas = UIBuilder.CreateCanvas("[Battle Banner]", 60);
            var banner = canvas.gameObject.AddComponent<BattleBanner>();
            banner._group = canvas.gameObject.AddComponent<CanvasGroup>();
            banner._group.blocksRaycasts = false;
            banner._group.interactable = false;

            banner._flash = UIBuilder.Image(canvas.transform, "Flash", Color.white);
            UIBuilder.Stretch(banner._flash.rectTransform);
            banner._flash.raycastTarget = false;

            var box = UIBuilder.Rect(canvas.transform, "Box");
            // Between the boss bar and the level-up cards, so neither hides it.
            box.anchorMin = new Vector2(0f, 0.63f);
            box.anchorMax = new Vector2(1f, 0.77f);
            box.offsetMin = box.offsetMax = Vector2.zero;

            banner._title = UIBuilder.Text(box, "", UITheme.TitleSize * 1.5f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
            banner._title.rectTransform.anchorMin = new Vector2(0f, 0.35f);
            banner._title.rectTransform.anchorMax = Vector2.one;
            banner._title.rectTransform.offsetMin = banner._title.rectTransform.offsetMax = Vector2.zero;
            banner._title.outlineWidth = 0.25f;
            banner._title.outlineColor = new Color32(0, 0, 0, 220);

            banner._subtitle = UIBuilder.Text(box, "", UITheme.HeaderSize, TextAlignmentOptions.Center, Color.white);
            banner._subtitle.rectTransform.anchorMin = Vector2.zero;
            banner._subtitle.rectTransform.anchorMax = new Vector2(1f, 0.35f);
            banner._subtitle.rectTransform.offsetMin = banner._subtitle.rectTransform.offsetMax = Vector2.zero;

            banner._group.alpha = 0f;
            return banner;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Play(string title, string subtitle, Color color, float hold, float flashAlpha)
        {
            _title.text = title;
            _title.color = color;
            _subtitle.text = subtitle;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Animate(color, hold, flashAlpha));
        }

        private IEnumerator Animate(Color color, float hold, float flashAlpha)
        {
            var titleRect = _title.rectTransform;
            _group.alpha = 1f;

            // Punch in + flash.
            const float punch = 0.25f;
            for (float t = 0f; t < punch; t += Time.unscaledDeltaTime)
            {
                float k = t / punch;
                titleRect.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, 1f - (1f - k) * (1f - k));
                _flash.color = new Color(color.r, color.g, color.b, flashAlpha * (1f - k));
                yield return null;
            }
            titleRect.localScale = Vector3.one;
            _flash.color = Color.clear;

            for (float t = 0f; t < hold; t += Time.unscaledDeltaTime) yield return null;

            const float fade = 0.35f;
            for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
            {
                _group.alpha = 1f - t / fade;
                yield return null;
            }
            _group.alpha = 0f;
            _routine = null;
        }
    }
}
