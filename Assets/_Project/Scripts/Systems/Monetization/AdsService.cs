using System;
using NinjaVillage.Core.Config;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NinjaVillage.Systems.Monetization
{
    /// <summary>Anything that can show ads. Real networks (LevelPlay...) plug in behind this later.</summary>
    public interface IAdsProvider
    {
        string Name { get; }
        bool IsRewardedReady { get; }
        /// <summary>onComplete(true) only when the player watched to the end and earned the reward.</summary>
        void ShowRewarded(string placement, Action<bool> onComplete);
        void ShowInterstitial(string placement, Action onClosed);
    }

    /// <summary>
    /// Ads (EPIC 21 "Revival ads", "Reward ads", "Remove Ads purchase"): rewarded ads are always optional
    /// and stay available after Remove Ads; interstitials are paced (Remote Config
    /// <c>interstitial_every_n_runs</c>, default 3), shown only when returning from a battle, and never
    /// after Remove Ads. Uses the <see cref="SimulatedAdsProvider"/> until an ad network is integrated.
    /// </summary>
    public static class AdsService
    {
        public const int AdGemsPerView = 5;
        public const int AdGemsDailyCap = 5;

        public static IAdsProvider Provider { get; set; } = new SimulatedAdsProvider();

        private static bool _interstitialQueued;

        public static bool AdsRemoved => SaveService.Data.Store.AdsRemoved;

        public static void ShowRewarded(string placement, Action<bool> onComplete)
        {
            if (Provider == null || !Provider.IsRewardedReady)
            {
                onComplete?.Invoke(false);
                return;
            }
            Provider.ShowRewarded(placement, watched =>
            {
                if (watched) Progress.Report(ProgressStatIds.AdWatched, 1, placement);
                onComplete?.Invoke(watched);
            });
        }

        // ---------------------------------------------------------------- free gems

        public static int AdGemsLeftToday
        {
            get
            {
                var store = SaveService.Data.Store;
                return store.AdGemsDay == GameClock.Today ? Mathf.Max(0, AdGemsDailyCap - store.AdGemsClaimed) : AdGemsDailyCap;
            }
        }

        public static void WatchForGems(Action<bool> onComplete)
        {
            if (AdGemsLeftToday <= 0)
            {
                onComplete?.Invoke(false);
                return;
            }
            ShowRewarded("free_gems", watched =>
            {
                if (watched)
                {
                    var store = SaveService.Data.Store;
                    if (store.AdGemsDay != GameClock.Today)
                    {
                        store.AdGemsDay = GameClock.Today;
                        store.AdGemsClaimed = 0;
                    }
                    store.AdGemsClaimed++;
                    Economy.CurrencyService.Grant(Economy.CurrencyType.Gems, AdGemsPerView, "ad_gems");
                    SaveService.SaveNow();
                }
                onComplete?.Invoke(watched);
            });
        }

        // ---------------------------------------------------------------- interstitial pacing

        /// <summary>Pure pacing rule (tested): show after every N completed runs, never with Remove Ads.</summary>
        public static bool ShouldShowInterstitial(int runsSinceLast, int everyNRuns, bool adsRemoved) =>
            !adsRemoved && everyNRuns > 0 && runsSinceLast >= everyNRuns;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            EventBus<RunEndedEvent>.UnsubscribePersistent(OnRunEnded);
            EventBus<SceneChangingEvent>.UnsubscribePersistent(OnSceneChanging);
            EventBus<RunEndedEvent>.SubscribePersistent(OnRunEnded);
            EventBus<SceneChangingEvent>.SubscribePersistent(OnSceneChanging);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnRunEnded(RunEndedEvent evt)
        {
            SaveService.Data.Store.RunsSinceInterstitial++;
            SaveService.MarkDirty();
        }

        // Only when leaving a battle for a menu — never interrupting play.
        private static void OnSceneChanging(SceneChangingEvent evt)
        {
            if (evt.FromScene != SceneNames.Battle || evt.ToScene == SceneNames.Battle) return;
            var store = SaveService.Data.Store;
            int every = RemoteValues.GetInt("interstitial_every_n_runs", 3);
            _interstitialQueued = ShouldShowInterstitial(store.RunsSinceInterstitial, every, store.AdsRemoved);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_interstitialQueued || Provider == null) return;
            _interstitialQueued = false;
            SaveService.Data.Store.RunsSinceInterstitial = 0;
            SaveService.MarkDirty();
            Provider.ShowInterstitial("post_run", null);
        }
    }
}
