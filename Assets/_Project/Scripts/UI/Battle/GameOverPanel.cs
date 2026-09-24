using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Monetization;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Battle
{
    /// <summary>
    /// End-of-run screen: victory/defeat title, wave reached, the run summary (kills, coins, time) and
    /// exits to the Village or Home in addition to the scene's Retry button. The summary text and exit
    /// buttons are created in code if the scene doesn't provide them, so older scenes keep working.
    /// </summary>
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text waveText;
        [Tooltip("Optional — created automatically under the panel when empty.")]
        [SerializeField] private TMP_Text summaryText;

        private bool _extrasBuilt;
        private UnityEngine.UI.Button _doubleCoins;
        private int _lastRunCoins;
        private bool _doubled;

        private void Awake()
        {
            // Subscribe before disabling panelRoot below — if this script lives on
            // panelRoot itself, OnEnable would never fire once it's inactive.
            EventBus<RunEndedEvent>.Subscribe(OnRunEnded);

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void OnDestroy() => EventBus<RunEndedEvent>.Unsubscribe(OnRunEnded);

        private void OnRunEnded(RunEndedEvent evt)
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(true);
            Time.timeScale = 0f;

            if (titleText != null)
                titleText.text = evt.Victory ? "VICTORY!" : "DEFEATED";
            if (waveText != null)
                waveText.text = $"Wave {evt.WaveReached}";

            BuildExtras();
            _lastRunCoins = evt.Summary != null ? evt.Summary.CoinsEarned : 0;
            _doubled = false;
            if (_doubleCoins != null) _doubleCoins.gameObject.SetActive(_lastRunCoins > 0);
            if (summaryText != null && evt.Summary != null)
            {
                var s = evt.Summary;
                int seconds = Mathf.RoundToInt(s.DurationSeconds);
                summaryText.text = $"Demons defeated: {s.Kills}   Bosses: {s.BossesKilled}\n" +
                                   $"Coins earned: {s.CoinsEarned}   Time: {seconds / 60}:{seconds % 60:00}";
            }
        }

        private void BuildExtras()
        {
            if (_extrasBuilt) return;
            _extrasBuilt = true;
            var root = panelRoot.transform;

            if (summaryText == null)
            {
                var text = UIBuilder.Text(root, "", UITheme.BodySize, TextAlignmentOptions.Center);
                var rt = text.rectTransform;
                rt.anchorMin = new Vector2(0.08f, 0.40f);
                rt.anchorMax = new Vector2(0.92f, 0.50f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                summaryText = text;
            }

            var row = UIBuilder.Horizontal(root, "ExitButtons", 24f);
            var rowRect = (RectTransform)row.transform;
            rowRect.anchorMin = new Vector2(0.1f, 0.12f);
            rowRect.anchorMax = new Vector2(0.9f, 0.2f);
            rowRect.offsetMin = rowRect.offsetMax = Vector2.zero;

            // Reward ad (EPIC 21 "Reward ads"): optional, doubles this run's coins once.
            _doubleCoins = UIBuilder.Button(root, "2x Coins (watch ad)", OnDoubleCoins, UITheme.Gold);
            var doubleRect = (RectTransform)_doubleCoins.transform;
            doubleRect.anchorMin = new Vector2(0.2f, 0.22f);
            doubleRect.anchorMax = new Vector2(0.8f, 0.29f);
            doubleRect.offsetMin = doubleRect.offsetMax = Vector2.zero;

            var gameManager = FindAnyObjectByType<GameManager>();
            UIBuilder.Button(row.transform, "Village", () =>
            {
                if (gameManager != null) gameManager.ReturnToVillage();
                else SceneLoader.LoadVillage();
            }, UITheme.ButtonSecondary);
            UIBuilder.Button(row.transform, "Home", () =>
            {
                if (gameManager != null) gameManager.ReturnToMainMenu();
                else SceneLoader.LoadMainMenu();
            }, UITheme.ButtonSecondary);
        }

        private void OnDoubleCoins()
        {
            if (_doubled || _lastRunCoins <= 0) return;
            _doubleCoins.interactable = false;
            AdsService.ShowRewarded("double_coins", watched =>
            {
                if (!watched)
                {
                    if (_doubleCoins != null) _doubleCoins.interactable = true;
                    return;
                }
                _doubled = true;
                CurrencyService.Grant(CurrencyType.Coins, _lastRunCoins, "double_coins_ad");
                NinjaVillage.Core.Audio.Sfx.Play(NinjaVillage.Core.Audio.AudioCueIds.RewardClaim);
                if (_doubleCoins != null) UIBuilder.SetLabel(_doubleCoins, $"+{_lastRunCoins} coins!");
            });
        }
    }
}
