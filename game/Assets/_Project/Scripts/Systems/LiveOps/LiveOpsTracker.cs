using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Bosses;
using NinjaVillage.Systems.Daily;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Meta;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.LiveOps
{
    /// <summary>
    /// Feeds live ops from gameplay (persistent, auto-created):
    /// <list type="bullet">
    /// <item><b>Battle pass XP</b>: RunEndedEvent → <see cref="LiveOpsRules.RunXp"/>; BossDefeatedEvent → 40;
    /// ProgressStatEvent QuestCompleted → 100, DailyLoginClaimed → 50, EvolutionDiscovered → 150.
    /// Runs are counted only via RunEndedEvent (never a progress stat), so nothing double-counts.</item>
    /// <item><b>Event missions</b>: same sourcing rules as daily quests (<see cref="DailyRules"/>): combat
    /// stats from combat events, everything else from ProgressStatEvent. Completing a mission grants pass
    /// XP directly and does NOT report QuestCompleted, so it can't also feed daily quest chains.</item>
    /// </list>
    /// </summary>
    public class LiveOpsTracker : MonoBehaviour
    {
        public const int BossXp = 40;
        public const int QuestXp = 100;
        public const int LoginXp = 50;
        public const int EvolutionXp = 150;
        public const int MissionXp = 150;

        private static LiveOpsTracker _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("[LiveOpsTracker]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<LiveOpsTracker>();
        }

        private void OnEnable()
        {
            EventBus<ProgressStatEvent>.SubscribePersistent(OnProgressStat);
            EventBus<EnemyKilledEvent>.SubscribePersistent(OnEnemyKilled);
            EventBus<BossDefeatedEvent>.SubscribePersistent(OnBossDefeated);
            EventBus<RunEndedEvent>.SubscribePersistent(OnRunEnded);
        }

        private void OnDisable()
        {
            EventBus<ProgressStatEvent>.UnsubscribePersistent(OnProgressStat);
            EventBus<EnemyKilledEvent>.UnsubscribePersistent(OnEnemyKilled);
            EventBus<BossDefeatedEvent>.UnsubscribePersistent(OnBossDefeated);
            EventBus<RunEndedEvent>.UnsubscribePersistent(OnRunEnded);
        }

        private void OnProgressStat(ProgressStatEvent evt)
        {
            if (evt.StatId == ProgressStatIds.QuestCompleted) BattlePassService.AddXp(QuestXp * Mathf.Max(1, evt.Amount), evt.Subject);
            else if (evt.StatId == ProgressStatIds.DailyLoginClaimed) BattlePassService.AddXp(LoginXp);
            else if (evt.StatId == ProgressStatIds.EvolutionDiscovered) BattlePassService.AddXp(EvolutionXp, evt.Subject);

            if (!DailyRules.IsCombatDerivedStat(evt.StatId)) RecordMission(evt.StatId, evt.Amount);
        }

        private void OnEnemyKilled(EnemyKilledEvent evt) => RecordMission(ProgressStatIds.EnemiesKilled, 1);

        private void OnBossDefeated(BossDefeatedEvent evt)
        {
            BattlePassService.AddXp(BossXp, evt.Definition != null ? evt.Definition.Id : null);
            RecordMission(ProgressStatIds.BossesKilled, 1);
        }

        private void OnRunEnded(RunEndedEvent evt)
        {
            BattlePassService.AddXp(LiveOpsRules.RunXp(evt.WaveReached, evt.Victory), "run");
            RecordMission(ProgressStatIds.RunCompleted, 1);
            if (evt.Victory) RecordMission(ProgressStatIds.RunVictory, 1);
            RecordMission(ProgressStatIds.WaveReached, evt.WaveReached);
        }

        private void RecordMission(string statId, int amount)
        {
            if (amount <= 0) return;
            var active = LiveOpsService.EnsureEvent();
            if (active == null) return;

            bool isMax = DailyRules.IsMaxStat(statId);
            bool changed = false;
            foreach (var progress in SaveService.Data.LiveOps.EventMissions)
            {
                if (progress == null || progress.Completed) continue;
                var mission = active.GetMission(progress.Id);
                if (mission == null || mission.statId != statId) continue;

                progress.Progress = Mathf.Min(mission.target, DailyRules.ApplyProgress(progress.Progress, amount, isMax));
                changed = true;
                if (progress.Progress >= mission.target)
                {
                    progress.Completed = true;
                    BattlePassService.AddXp(MissionXp, mission.id);
                }
            }
            if (!changed) return;
            SaveService.MarkDirty();
            EventBus<LiveOpsChangedEvent>.Raise(new LiveOpsChangedEvent());
        }
    }

    /// <summary>Applies the active event's coin/XP bonus plus Remote Config global multipliers at run start.</summary>
    public sealed class LiveOpsRunModifier : IRunStartModifier
    {
        public int Order => RunModifierOrder.LiveOps;

        public void Apply(RunStartContext context)
        {
            var stats = context.Stats;
            if (stats == null) return;

            var evt = LiveOpsService.ActiveEvent;
            if (evt != null)
            {
                if (evt.CoinBonus != 0f) stats.AddGoldBonusMultiplier(evt.CoinBonus);
                if (evt.XpBonus != 0f) stats.AddXpGainMultiplier(evt.XpBonus);
            }

            // Remote Config global tuning (1.0 = unchanged).
            float coin = NinjaVillage.Core.Config.RemoteValues.GetFloat("coin_multiplier", 1f);
            float xp = NinjaVillage.Core.Config.RemoteValues.GetFloat("xp_multiplier", 1f);
            if (!Mathf.Approximately(coin, 1f)) stats.AddGoldBonusMultiplier(coin - 1f);
            if (!Mathf.Approximately(xp, 1f)) stats.AddXpGainMultiplier(xp - 1f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => RunStartModifiers.Register(new LiveOpsRunModifier());
    }
}
