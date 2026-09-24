using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Bosses;
using NinjaVillage.Gameplay.Skills;
using NinjaVillage.Gameplay.Ultimates;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Daily
{
    /// <summary>
    /// Feeds quest and achievement progress from gameplay, persistently across scenes.
    ///
    /// Sources (so nothing is counted twice — see <see cref="DailyRules.IsCombatDerivedStat"/>):
    /// <list type="bullet">
    /// <item>Combat stats come from combat events: EnemyKilledEvent → enemies_killed,
    /// BossDefeatedEvent → bosses_killed, RunEndedEvent → run_completed / run_victory / wave_reached /
    /// survived_seconds, SkillLeveledEvent → skill_picked, UltimateActivatedEvent → ultimate_used.</item>
    /// <item>Everything else (upgrades, purchases, coins earned/spent, chests, logins, completed quests...)
    /// comes from <see cref="ProgressStatEvent"/>.</item>
    /// </list>
    /// Completing a quest reports <see cref="ProgressStatIds.QuestCompleted"/> (for achievements and
    /// the battle pass). Villager requests (<see cref="Requests.RequestService"/>) get every stat too.
    /// Writes are batched with SaveService.MarkDirty.
    /// </summary>
    public class QuestTracker : MonoBehaviour
    {
        private static QuestTracker _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("[QuestTracker]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<QuestTracker>();
        }

        private void OnEnable()
        {
            EventBus<ProgressStatEvent>.SubscribePersistent(OnProgressStat);
            EventBus<EnemyKilledEvent>.SubscribePersistent(OnEnemyKilled);
            EventBus<BossDefeatedEvent>.SubscribePersistent(OnBossDefeated);
            EventBus<RunEndedEvent>.SubscribePersistent(OnRunEnded);
            EventBus<SkillLeveledEvent>.SubscribePersistent(OnSkillLeveled);
            EventBus<UltimateActivatedEvent>.SubscribePersistent(OnUltimate);
        }

        private void OnDisable()
        {
            EventBus<ProgressStatEvent>.UnsubscribePersistent(OnProgressStat);
            EventBus<EnemyKilledEvent>.UnsubscribePersistent(OnEnemyKilled);
            EventBus<BossDefeatedEvent>.UnsubscribePersistent(OnBossDefeated);
            EventBus<RunEndedEvent>.UnsubscribePersistent(OnRunEnded);
            EventBus<SkillLeveledEvent>.UnsubscribePersistent(OnSkillLeveled);
            EventBus<UltimateActivatedEvent>.UnsubscribePersistent(OnUltimate);
        }

        private void OnProgressStat(ProgressStatEvent evt)
        {
            if (DailyRules.IsCombatDerivedStat(evt.StatId)) return;
            Record(evt.StatId, evt.Amount);
        }

        private void OnEnemyKilled(EnemyKilledEvent evt) => Record(ProgressStatIds.EnemiesKilled, 1);
        private void OnBossDefeated(BossDefeatedEvent evt) => Record(ProgressStatIds.BossesKilled, 1);
        private void OnSkillLeveled(SkillLeveledEvent evt) => Record(ProgressStatIds.SkillPicked, 1);
        private void OnUltimate(UltimateActivatedEvent evt) => Record(ProgressStatIds.UltimateUsed, 1);

        private void OnRunEnded(RunEndedEvent evt)
        {
            Record(ProgressStatIds.RunCompleted, 1);
            if (evt.Victory) Record(ProgressStatIds.RunVictory, 1);
            Record(ProgressStatIds.WaveReached, evt.WaveReached);
            if (evt.Summary != null) Record(ProgressStatIds.SurvivedSeconds, Mathf.RoundToInt(evt.Summary.DurationSeconds));
        }

        private readonly List<string> _completedBuffer = new();

        /// <summary>Applies one stat report to today's/this week's quests and every achievement on that stat.</summary>
        public void Record(string statId, int amount)
        {
            if (string.IsNullOrEmpty(statId) || amount <= 0) return;
            QuestService.EnsureRolled();

            bool isMax = DailyRules.IsMaxStat(statId);
            Requests.RequestService.Record(statId, amount, isMax); // villager requests count the same stats
            bool changed = false;
            _completedBuffer.Clear();

            var daily = SaveService.Data.Daily;
            changed |= Apply(daily.DailyQuests, statId, amount, isMax);
            changed |= Apply(daily.WeeklyQuests, statId, amount, isMax);

            var achievements = QuestService.Achievements;
            if (achievements != null)
            {
                foreach (var achievement in achievements.All)
                {
                    if (achievement == null || achievement.StatId != statId) continue;
                    var progress = QuestService.AchievementProgress(achievement);
                    int updated = DailyRules.ApplyProgress(progress.Progress, amount, isMax);
                    if (updated == progress.Progress) continue;
                    progress.Progress = updated;
                    progress.Completed = DailyRules.ReachedTiers(updated, achievement.TierTargets) >= achievement.TierCount;
                    changed = true;
                }
            }

            if (!changed) return;
            SaveService.MarkDirty();
            EventBus<DailyContentChangedEvent>.Raise(new DailyContentChangedEvent());

            // Reported after the loop: QuestCompleted feeds achievements (and the battle pass) through
            // Record again, which reuses _completedBuffer — so report from a copy.
            if (_completedBuffer.Count == 0) return;
            var completed = _completedBuffer.ToArray();
            foreach (var questId in completed)
                Progress.Report(ProgressStatIds.QuestCompleted, 1, questId);
        }

        private bool Apply(List<QuestProgress> quests, string statId, int amount, bool isMax)
        {
            bool changed = false;
            foreach (var progress in quests)
            {
                if (progress == null || progress.Completed) continue;
                var quest = QuestService.GetQuest(progress.Id);
                if (quest == null || quest.StatId != statId) continue;

                int updated = Mathf.Min(quest.Target, DailyRules.ApplyProgress(progress.Progress, amount, isMax));
                if (updated == progress.Progress) continue;
                progress.Progress = updated;
                changed = true;
                if (updated >= quest.Target)
                {
                    progress.Completed = true;
                    _completedBuffer.Add(quest.Id);
                }
            }
            return changed;
        }
    }
}
