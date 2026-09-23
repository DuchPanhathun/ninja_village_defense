using System;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Bosses;
using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Meta
{
    /// <summary>
    /// Counts what happened during one battle run (kills, bosses, coins, time) so the
    /// GameManager can record it into the profile and quests/analytics can read it.
    /// Lives next to the GameManager; added automatically if missing.
    /// </summary>
    public class RunStatsTracker : MonoBehaviour
    {
        public int Kills { get; private set; }
        public int BossesKilled { get; private set; }
        public float StartTime { get; private set; }
        public float ElapsedSeconds => Time.time - StartTime;

        private int _coinsAtStart;

        private void Start()
        {
            StartTime = Time.time;
            _coinsAtStart = SaveService.Data.Wallet.Coins;
        }

        private void OnEnable()
        {
            EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
            EventBus<BossDefeatedEvent>.Subscribe(OnBossDefeated);
        }

        private void OnDisable()
        {
            EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
            EventBus<BossDefeatedEvent>.Unsubscribe(OnBossDefeated);
        }

        private void OnEnemyKilled(EnemyKilledEvent evt) => Kills++;
        private void OnBossDefeated(BossDefeatedEvent evt) => BossesKilled++;

        public int CoinsEarned => Mathf.Max(0, SaveService.Data.Wallet.Get(CurrencyType.Coins) - _coinsAtStart);

        public RunRecord BuildRecord(bool victory, int waveReached)
        {
            string weaponId = null;
            if (PlayerReference.Instance != null &&
                PlayerReference.Instance.TryGetComponent<AutoAttackController>(out var attack) &&
                attack.Weapon != null)
            {
                weaponId = attack.Weapon.Definition.Id;
            }

            return new RunRecord
            {
                EndedUtcTicks = DateTime.UtcNow.Ticks,
                Victory = victory,
                WaveReached = waveReached,
                Kills = Kills,
                BossesKilled = BossesKilled,
                CoinsEarned = CoinsEarned,
                DurationSeconds = ElapsedSeconds,
                HeroId = SaveService.Data.Heroes.SelectedHeroId,
                WeaponId = weaponId,
            };
        }
    }
}
