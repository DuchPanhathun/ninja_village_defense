using System;
using UnityEngine;

namespace NinjaVillage.Systems.Monetization
{
    /// <summary>
    /// Stand-in ad network until a real one (e.g. LevelPlay) is integrated: shows a full-screen
    /// "advertisement" overlay with a countdown, on unscaled time so it works while the game is paused.
    /// Rewarded: closing after the countdown grants the reward, skipping early doesn't. Every flow that
    /// uses ads (revive, double coins, free gems, interstitial pacing) can be tested end to end with it.
    /// </summary>
    public sealed class SimulatedAdsProvider : IAdsProvider
    {
        public float RewardedSeconds = 3f;
        public float InterstitialSeconds = 2f;

        public string Name => "Simulated";
        public bool IsRewardedReady => true;

        public void ShowRewarded(string placement, Action<bool> onComplete) =>
            SimulatedAdOverlay.Show($"Rewarded ad ({placement})", RewardedSeconds, true, onComplete);

        public void ShowInterstitial(string placement, Action onClosed) =>
            SimulatedAdOverlay.Show($"Ad break ({placement})", InterstitialSeconds, false, _ => onClosed?.Invoke());
    }
}
