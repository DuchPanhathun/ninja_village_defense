using System.Collections.Generic;
using System.Threading.Tasks;
using NinjaVillage.Core.Config;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Backend
{
    /// <summary>
    /// Global leaderboard (EPIC 22 "Leaderboard"): submits the player's best run (wave, then kills) when a
    /// run beats it, and fetches the top list for the Leaderboard screen. The best run is kept in this
    /// device's PlayerPrefs with a "submitted" flag, so an upload that failed offline retries on the next sign-in.
    /// </summary>
    public static class LeaderboardService
    {
        public const int TopCount = 50;
        private const string BestWaveKey = "nv_lb_best_wave";
        private const string BestKillsKey = "nv_lb_best_kills";
        private const string SubmittedKey = "nv_lb_submitted";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            EventBus<RunEndedEvent>.UnsubscribePersistent(OnRunEnded);
            EventBus<RunEndedEvent>.SubscribePersistent(OnRunEnded);
            EventBus<SignedInEvent>.UnsubscribePersistent(OnSignedIn);
            EventBus<SignedInEvent>.SubscribePersistent(OnSignedIn);
        }

        public static int BestWave => PlayerPrefs.GetInt(BestWaveKey, 0);
        public static int BestKills => PlayerPrefs.GetInt(BestKillsKey, 0);

        private static void OnRunEnded(RunEndedEvent evt)
        {
            int kills = evt.Summary != null ? evt.Summary.Kills : 0;
            if (!BackendRules.IsNewBest(BestWave, BestKills, evt.WaveReached, kills)) return;
            PlayerPrefs.SetInt(BestWaveKey, evt.WaveReached);
            PlayerPrefs.SetInt(BestKillsKey, kills);
            PlayerPrefs.SetInt(SubmittedKey, 0);
            PlayerPrefs.Save();
            _ = SubmitBestAsync();
        }

        // Retry a best that couldn't be uploaded (offline at the time) once we're signed in again.
        private static void OnSignedIn(SignedInEvent evt)
        {
            if (PlayerPrefs.GetInt(SubmittedKey, 1) == 0) _ = SubmitBestAsync();
        }

        public static async Task SubmitBestAsync()
        {
            var provider = BackendService.Provider;
            var user = provider?.CurrentUser;
            if (provider == null || user == null || !provider.IsOnline || !RemoteValues.GetBool("leaderboard_enabled", true)) return;

            bool ok = await provider.SubmitLeaderboardAsync(new LeaderboardEntry
            {
                UserId = user.UserId,
                DisplayName = SaveService.Data.Profile.DisplayName,
                BestWave = BestWave,
                BestKills = BestKills,
            });
            if (ok)
            {
                PlayerPrefs.SetInt(SubmittedKey, 1);
                PlayerPrefs.Save();
            }
        }

        public static Task<List<LeaderboardEntry>> FetchTopAsync()
        {
            var provider = BackendService.Provider;
            return provider != null ? provider.FetchLeaderboardAsync(TopCount) : Task.FromResult(new List<LeaderboardEntry>());
        }
    }
}
