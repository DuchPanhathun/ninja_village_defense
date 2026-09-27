using System.Collections;
using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Monetization;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinjaVillage.UI.Battle
{
    /// <summary>
    /// "Continue?" prompt (EPIC 21 "Revival ads"): when the player dies the first time in a run, offers to
    /// watch a rewarded ad to revive with half health. Declining — or letting the 8-second countdown run
    /// out — ends the run normally. Built in code whenever <see cref="RevivePromptEvent"/> fires, so the
    /// Battle scene needs no setup.
    /// </summary>
    public class RevivePromptUI : MonoBehaviour
    {
        public const float DecisionSeconds = 8f;

        private GameManager _gameManager;
        private TextMeshProUGUI _countdown;
        private bool _resolved;
        private bool _watching;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            EventBus<RevivePromptEvent>.UnsubscribePersistent(OnPrompt);
            EventBus<RevivePromptEvent>.SubscribePersistent(OnPrompt);
        }

        private static void OnPrompt(RevivePromptEvent evt)
        {
            var gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager == null) return;

            var canvas = UIBuilder.CreateCanvas("[Revive Prompt]", 80);
            var ui = canvas.gameObject.AddComponent<RevivePromptUI>();
            ui._gameManager = gameManager;

            var backdrop = UIBuilder.Panel(canvas.transform, "Backdrop", new Color(0.05f, 0f, 0f, 0.8f));
            var card = UIBuilder.Panel(backdrop, "Card", UITheme.Panel);
            card.anchorMin = new Vector2(0.08f, 0.3f);
            card.anchorMax = new Vector2(0.92f, 0.7f);
            card.offsetMin = card.offsetMax = Vector2.zero;

            var column = UIBuilder.Vertical(card, "Column", 24f, 40, TextAnchor.MiddleCenter);
            UIBuilder.Stretch((RectTransform)column.transform);
            UIBuilder.Text(column.transform, "YOU FELL!", UITheme.TitleSize, TextAlignmentOptions.Center, UITheme.Negative, FontStyles.Bold);
            UIBuilder.Text(column.transform, "Watch a short ad to get back up with half your health?", UITheme.BodySize, TextAlignmentOptions.Center);
            ui._countdown = UIBuilder.Text(column.transform, "", UITheme.HeaderSize, TextAlignmentOptions.Center, UITheme.Gold);
            UIBuilder.Button(column.transform, "Revive (watch ad)", ui.OnRevive, UITheme.Button, 130f);
            UIBuilder.Button(column.transform, "No thanks", ui.OnDecline, UITheme.ButtonSecondary, 100f);

            ui.StartCoroutine(ui.Countdown());
        }

        private IEnumerator Countdown()
        {
            for (float left = DecisionSeconds; left > 0f; left -= Time.unscaledDeltaTime)
            {
                if (_watching) yield break;
                _countdown.text = Mathf.CeilToInt(left).ToString();
                yield return null;
            }
            OnDecline();
        }

        private void OnRevive()
        {
            if (_resolved || _watching) return;
            _watching = true;
            Sfx.Play(AudioCueIds.UiClick);
            AdsService.ShowRewarded("revive", watched =>
            {
                if (watched) Resolve(accept: true);
                else Resolve(accept: false);
            });
        }

        private void OnDecline()
        {
            if (_watching) return;
            Resolve(accept: false);
        }

        private void Resolve(bool accept)
        {
            if (_resolved) return;
            _resolved = true;
            if (_gameManager != null)
            {
                if (accept) _gameManager.AcceptRevive();
                else _gameManager.DeclineRevive();
            }
            if (accept) Sfx.Play(AudioCueIds.RewardClaim);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // Scene torn down while the prompt was open: make sure the run still ends properly.
            if (!_resolved && _gameManager != null) _gameManager.DeclineRevive();
        }
    }
}
