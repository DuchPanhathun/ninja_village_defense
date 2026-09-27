using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Bosses;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Waves;
using NinjaVillage.Systems.GameFlow;
using UnityEngine;

namespace NinjaVillage.Systems.Economy
{
    /// <summary>
    /// Battle rewards that aren't physical drops (EPIC 15 "Reward balancing"): a coin bonus for every
    /// cleared wave, gems for each boss, and a victory bonus. Enemy kills alone make early runs feel
    /// stingy and punish dying just before a wave ends; paying per cleared wave rewards progress
    /// itself and gives the economy a knob that scales with how far the player got.
    ///
    /// Registered before the first scene with persistent subscriptions (it's static and must
    /// survive SceneLoader's EventBusRegistry.ClearAll). Amounts come from <see cref="EconomyConfig"/>.
    /// </summary>
    public static class RunRewardService
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            // Unsubscribe first so a disabled domain reload can't double-register.
            EventBus<WaveClearedEvent>.UnsubscribePersistent(OnWaveCleared);
            EventBus<BossDefeatedEvent>.UnsubscribePersistent(OnBossDefeated);
            EventBus<RunEndedEvent>.UnsubscribePersistent(OnRunEnded);

            EventBus<WaveClearedEvent>.SubscribePersistent(OnWaveCleared);
            EventBus<BossDefeatedEvent>.SubscribePersistent(OnBossDefeated);
            EventBus<RunEndedEvent>.SubscribePersistent(OnRunEnded);
        }

        private static void OnWaveCleared(WaveClearedEvent evt)
        {
            int coins = EconomyConfig.Current.WaveClearCoins(evt.WaveNumber);
            coins = Mathf.RoundToInt(coins * GoldMultiplier());
            if (coins <= 0) return;

            CurrencyService.Grant(CurrencyType.Coins, coins, "wave_clear");
            Sfx.Play(AudioCueIds.RewardClaim, 0.6f);
        }

        private static void OnBossDefeated(BossDefeatedEvent evt)
        {
            int gems = EconomyConfig.Current.GemsPerBossKill;
            if (gems > 0)
                CurrencyService.Grant(CurrencyType.Gems, gems, evt.Definition != null ? evt.Definition.Id : "boss");
        }

        private static void OnRunEnded(RunEndedEvent evt)
        {
            if (!evt.Victory) return;
            int coins = Mathf.RoundToInt(EconomyConfig.Current.VictoryBonusCoins * GoldMultiplier());
            CurrencyService.Grant(CurrencyType.Coins, coins, "victory");
        }

        /// <summary>The Gold Bonus skill and the Shrine's Prosperity blessing both live on PlayerStats.</summary>
        private static float GoldMultiplier()
        {
            if (PlayerReference.Instance != null && PlayerReference.Instance.TryGetComponent<PlayerStats>(out var stats))
                return Mathf.Max(0f, stats.GoldBonusMultiplier);
            return 1f;
        }
    }
}
