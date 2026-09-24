using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Chapters;
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
    /// exits to the Village or Home in addition to the scene's Retry button. In a chapter it reads
    /// "CHAPTER N CLEAR!", lists the first-clear reward and offers "Next Chapter". The summary text and exit
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
        private UnityEngine.UI.Button _nextChapter;
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

            var chapter = ChapterService.LastResult;
            var chapterDef = chapter != null ? ChapterService.Get(chapter.ChapterId) : null;
            if (titleText != null)
                titleText.text = evt.Victory ? (chapter != null ? $"CHAPTER {chapter.ChapterNumber} CLEAR!" : "VICTORY!") : "DEFEATED";
            if (waveText != null)
                waveText.text = chapter != null
                    ? $"{(chapterDef != null ? chapterDef.DisplayName + "  ·  " : "")}Wave {Mathf.Min(evt.WaveReached, chapter.TotalWaves)}/{chapter.TotalWaves}"
                    : $"Wave {evt.WaveReached}";

            BuildExtras();
            var next = chapter != null && evt.Victory ? ChapterService.GetByNumber(chapter.ChapterNumber + 1) : null;
            if (_nextChapter != null)
            {
                _nextChapter.gameObject.SetActive(next != null && ChapterService.IsUnlocked(next));
                if (next != null) UIBuilder.SetLabel(_nextChapter, $"Next: Chapter {next.Number}");
            }
            _lastRunCoins = evt.Summary != null ? evt.Summary.CoinsEarned : 0;
            _doubled = false;
            if (_doubleCoins != null) _doubleCoins.gameObject.SetActive(_lastRunCoins > 0);
            if (summaryText != null && evt.Summary != null)
            {
                var s = evt.Summary;
                int seconds = Mathf.RoundToInt(s.DurationSeconds);
                summaryText.text = $"Demons defeated: {s.Kills}   Bosses: {s.BossesKilled}\n" +
                                   $"Coins earned: {s.CoinsEarned}   Time: {seconds / 60}:{seconds % 60:00}";
                if (chapter != null && chapter.FirstClear)
                    summaryText.text += $"\n<color=#FFD24D>First clear reward: +{chapter.RewardCoins} coins  +{chapter.RewardGems} gems</color>";
                else if (chapter != null && chapter.NewBestWave && !evt.Victory)
                    summaryText.text += "\n<color=#FFD24D>New best wave!</color>";
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
                rt.anchorMin = new Vector2(0.08f, 0.445f);
                rt.anchorMax = new Vector2(0.92f, 0.545f);
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

            // Victory in a chapter: straight on to the next one (the clear already selected it).
            _nextChapter = UIBuilder.Button(root, "Next Chapter", () =>
            {
                var result = ChapterService.LastResult;
                var next = result != null ? ChapterService.GetByNumber(result.ChapterNumber + 1) : null;
                if (next != null) ChapterService.TrySelect(next);
                SceneLoader.LoadBattle();
            }, UITheme.Positive);
            var nextRect = (RectTransform)_nextChapter.transform;
            nextRect.anchorMin = new Vector2(0.2f, 0.385f); // between the summary and the scene's Retry button
            nextRect.anchorMax = new Vector2(0.8f, 0.435f);
            nextRect.offsetMin = nextRect.offsetMax = Vector2.zero;
            _nextChapter.gameObject.SetActive(false);

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
