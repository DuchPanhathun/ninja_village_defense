using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Bosses;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills;
using NinjaVillage.Gameplay.Ultimates;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Evolution;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Audio
{
    /// <summary>
    /// Turns gameplay events that already exist into sounds, so hits, deaths, level-ups and
    /// pickups are audible without gameplay scripts knowing about audio. Weapon/skill-specific
    /// cues (kunai throw, lightning...) are raised at their call sites instead, since only the
    /// weapon knows which sound it makes. Spam from 50 simultaneous deaths is absorbed by the
    /// AudioManager's per-cue cooldown and voice caps.
    /// </summary>
    public class GameplaySfxBridge : MonoBehaviour
    {
        private int _lastCoins = -1;

        private void OnEnable()
        {
            EventBus<EntityDamagedEvent>.SubscribePersistent(OnEntityDamaged);
            EventBus<EnemyKilledEvent>.SubscribePersistent(OnEnemyKilled);
            EventBus<PlayerDamagedEvent>.SubscribePersistent(OnPlayerDamaged);
            EventBus<PlayerDiedEvent>.SubscribePersistent(OnPlayerDied);
            EventBus<LevelUpEvent>.SubscribePersistent(OnLevelUp);
            EventBus<XpGainedEvent>.SubscribePersistent(OnXpGained);
            EventBus<SkillLeveledEvent>.SubscribePersistent(OnSkillLeveled);
            EventBus<UltimateActivatedEvent>.SubscribePersistent(OnUltimate);
            EventBus<BossSpawnedEvent>.SubscribePersistent(OnBossSpawned);
            EventBus<EvolutionUnlockedEvent>.SubscribePersistent(OnEvolution);
            EventBus<CurrencyChangedEvent>.SubscribePersistent(OnCurrencyChanged);
            EventBus<SaveLoadedEvent>.SubscribePersistent(OnSaveLoaded);
        }

        private void OnDisable()
        {
            EventBus<EntityDamagedEvent>.UnsubscribePersistent(OnEntityDamaged);
            EventBus<EnemyKilledEvent>.UnsubscribePersistent(OnEnemyKilled);
            EventBus<PlayerDamagedEvent>.UnsubscribePersistent(OnPlayerDamaged);
            EventBus<PlayerDiedEvent>.UnsubscribePersistent(OnPlayerDied);
            EventBus<LevelUpEvent>.UnsubscribePersistent(OnLevelUp);
            EventBus<XpGainedEvent>.UnsubscribePersistent(OnXpGained);
            EventBus<SkillLeveledEvent>.UnsubscribePersistent(OnSkillLeveled);
            EventBus<UltimateActivatedEvent>.UnsubscribePersistent(OnUltimate);
            EventBus<BossSpawnedEvent>.UnsubscribePersistent(OnBossSpawned);
            EventBus<EvolutionUnlockedEvent>.UnsubscribePersistent(OnEvolution);
            EventBus<CurrencyChangedEvent>.UnsubscribePersistent(OnCurrencyChanged);
            EventBus<SaveLoadedEvent>.UnsubscribePersistent(OnSaveLoaded);
        }

        private void OnEntityDamaged(EntityDamagedEvent evt)
        {
            // The player's own hits are voiced by PlayerDamagedEvent (PlayerHurt); skip them here.
            if (PlayerReference.Instance != null &&
                ((Vector2)PlayerReference.Instance.PlayerTransform.position - evt.Position).sqrMagnitude < 0.0001f)
                return;

            Sfx.PlayAt(evt.IsCritical ? AudioCueIds.CriticalHit : AudioCueIds.EnemyHit, evt.Position);
        }

        private void OnEnemyKilled(EnemyKilledEvent evt) => Sfx.PlayAt(AudioCueIds.EnemyDeath, evt.Position);
        private void OnPlayerDamaged(PlayerDamagedEvent evt) => Sfx.Play(AudioCueIds.PlayerHurt);
        private void OnPlayerDied(PlayerDiedEvent evt) => Sfx.Play(AudioCueIds.PlayerDeath);
        private void OnLevelUp(LevelUpEvent evt) => Sfx.Play(AudioCueIds.LevelUp);
        private void OnXpGained(XpGainedEvent evt) => Sfx.Play(AudioCueIds.XpPickup, 0.6f);
        private void OnSkillLeveled(SkillLeveledEvent evt) => Sfx.Play(AudioCueIds.SkillSelect);
        private void OnUltimate(UltimateActivatedEvent evt) => Sfx.Play(AudioCueIds.UltimateActivate);
        private void OnBossSpawned(BossSpawnedEvent evt) => Sfx.Play(AudioCueIds.BossRoar);
        private void OnEvolution(EvolutionUnlockedEvent evt) => Sfx.Play(AudioCueIds.EvolutionUnlock);

        private void OnSaveLoaded(SaveLoadedEvent evt) => _lastCoins = evt.Data.Wallet.Coins;

        private void OnCurrencyChanged(CurrencyChangedEvent evt)
        {
            if (evt.Type != CurrencyType.Coins) return;
            if (_lastCoins < 0) _lastCoins = evt.NewBalance;

            // Only gains jingle; purchases have their own UiPurchase cue.
            if (evt.NewBalance > _lastCoins) Sfx.Play(AudioCueIds.CoinPickup, 0.7f);
            _lastCoins = evt.NewBalance;
        }
    }
}
