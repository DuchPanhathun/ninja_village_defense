using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Bosses;
using NinjaVillage.Gameplay.Ultimates;
using UnityEngine;

namespace NinjaVillage.Systems.Settings
{
    /// <summary>
    /// Buzzes the phone for the few moments that deserve it — player death, a boss entering,
    /// unleashing an ultimate — so gameplay code never has to know about haptics or the
    /// Vibration setting. Rate-limited because Android vibrations last ~0.5s and stacking
    /// them feels broken. Listens persistently, so it works across scene loads.
    /// </summary>
    public static class HapticFeedback
    {
        private const float MinInterval = 0.6f;
        private static float _lastVibrateTime = -10f;

        /// <summary>Vibrates if the setting allows it and the last buzz was long enough ago.</summary>
        public static void Pulse()
        {
            float now = Time.unscaledTime;
            if (now - _lastVibrateTime < MinInterval) return;
            _lastVibrateTime = now;
            SettingsService.Vibrate();
        }

        private static void OnPlayerDied(PlayerDiedEvent evt) => Pulse();
        private static void OnBossSpawned(BossSpawnedEvent evt) => Pulse();
        private static void OnUltimate(UltimateActivatedEvent evt) => Pulse();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            _lastVibrateTime = -10f;
            // Unsubscribe first: with domain reload disabled the static handlers would stack.
            EventBus<PlayerDiedEvent>.UnsubscribePersistent(OnPlayerDied);
            EventBus<BossSpawnedEvent>.UnsubscribePersistent(OnBossSpawned);
            EventBus<UltimateActivatedEvent>.UnsubscribePersistent(OnUltimate);
            EventBus<PlayerDiedEvent>.SubscribePersistent(OnPlayerDied);
            EventBus<BossSpawnedEvent>.SubscribePersistent(OnBossSpawned);
            EventBus<UltimateActivatedEvent>.SubscribePersistent(OnUltimate);
        }
    }
}
