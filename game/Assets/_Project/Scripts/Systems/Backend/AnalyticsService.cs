using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Bosses;
using NinjaVillage.Gameplay.Skills;
using NinjaVillage.Systems.Evolution;
using NinjaVillage.Systems.GameFlow;
using UnityEngine;

namespace NinjaVillage.Systems.Backend
{
    /// <summary>
    /// Analytics (EPIC 0/22 "Analytics"): turns gameplay events into analytics events — run_start,
    /// run_end (victory, wave, kills, duration, coins), boss_defeated, level_up, skill_picked,
    /// evolution_discovered, screen_view on scene changes, and every <see cref="ProgressStatEvent"/>
    /// (upgrades, purchases, ads, quests...) as <c>stat_&lt;id&gt;</c>. Names/params are sanitized to
    /// Firebase limits (<see cref="BackendRules.SanitizeName"/>). Players can opt out (stored in
    /// PlayerPrefs, applied on sign-in).
    /// </summary>
    public static class AnalyticsService
    {
        private const string OptOutKey = "nv_analytics_opt_out";

        public static bool OptedOut
        {
            get => PlayerPrefs.GetInt(OptOutKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(OptOutKey, value ? 1 : 0);
                PlayerPrefs.Save();
                ApplyConsent();
            }
        }

        public static void ApplyConsent() => BackendService.Provider?.SetAnalyticsEnabled(!OptedOut);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            Unregister();
            EventBus<RunStartedEvent>.SubscribePersistent(OnRunStarted);
            EventBus<RunEndedEvent>.SubscribePersistent(OnRunEnded);
            EventBus<BossDefeatedEvent>.SubscribePersistent(OnBossDefeated);
            EventBus<LevelUpEvent>.SubscribePersistent(OnLevelUp);
            EventBus<SkillLeveledEvent>.SubscribePersistent(OnSkillLeveled);
            EventBus<EvolutionUnlockedEvent>.SubscribePersistent(OnEvolution);
            EventBus<ProgressStatEvent>.SubscribePersistent(OnProgressStat);
            EventBus<SceneChangingEvent>.SubscribePersistent(OnSceneChanging);
        }

        private static void Unregister()
        {
            EventBus<RunStartedEvent>.UnsubscribePersistent(OnRunStarted);
            EventBus<RunEndedEvent>.UnsubscribePersistent(OnRunEnded);
            EventBus<BossDefeatedEvent>.UnsubscribePersistent(OnBossDefeated);
            EventBus<LevelUpEvent>.UnsubscribePersistent(OnLevelUp);
            EventBus<SkillLeveledEvent>.UnsubscribePersistent(OnSkillLeveled);
            EventBus<EvolutionUnlockedEvent>.UnsubscribePersistent(OnEvolution);
            EventBus<ProgressStatEvent>.UnsubscribePersistent(OnProgressStat);
            EventBus<SceneChangingEvent>.UnsubscribePersistent(OnSceneChanging);
        }

        /// <summary>Logs a custom event through the active provider (no-op when opted out).</summary>
        public static void Log(string name, params AnalyticsParam[] parameters)
        {
            var provider = BackendService.Provider;
            if (provider == null || OptedOut) return;
            provider.LogEvent(BackendRules.SanitizeName(name), BackendRules.SanitizeParams(parameters));
        }

        private static void OnRunStarted(RunStartedEvent evt) =>
            Log("run_start", new AnalyticsParam("hero", evt.HeroId ?? "none"), new AnalyticsParam("weapon", evt.WeaponId ?? "none"));

        private static void OnRunEnded(RunEndedEvent evt)
        {
            var p = new List<AnalyticsParam>
            {
                new("victory", evt.Victory ? 1L : 0L),
                new("wave", (long)evt.WaveReached),
            };
            if (evt.Summary != null)
            {
                p.Add(new AnalyticsParam("kills", (long)evt.Summary.Kills));
                p.Add(new AnalyticsParam("bosses", (long)evt.Summary.BossesKilled));
                p.Add(new AnalyticsParam("coins", (long)evt.Summary.CoinsEarned));
                p.Add(new AnalyticsParam("duration_s", (long)evt.Summary.DurationSeconds));
                p.Add(new AnalyticsParam("hero", evt.Summary.HeroId ?? "none"));
            }
            Log("run_end", p.ToArray());
        }

        private static void OnBossDefeated(BossDefeatedEvent evt) =>
            Log("boss_defeated", new AnalyticsParam("boss", evt.Definition != null ? evt.Definition.Id : "unknown"));

        private static void OnLevelUp(LevelUpEvent evt) => Log("level_up", new AnalyticsParam("level", (long)evt.NewLevel));

        private static void OnSkillLeveled(SkillLeveledEvent evt) =>
            Log("skill_picked", new AnalyticsParam("skill", evt.Skill != null ? evt.Skill.Id : "unknown"), new AnalyticsParam("level", (long)evt.NewLevel));

        private static void OnEvolution(EvolutionUnlockedEvent evt) =>
            Log("evolution_discovered", new AnalyticsParam("recipe", evt.Recipe != null ? evt.Recipe.Id : "unknown"));

        private static void OnProgressStat(ProgressStatEvent evt) =>
            Log("stat_" + evt.StatId, new AnalyticsParam("amount", (long)evt.Amount), new AnalyticsParam("subject", evt.Subject ?? string.Empty));

        private static void OnSceneChanging(SceneChangingEvent evt) =>
            Log("screen_view", new AnalyticsParam("screen_name", evt.ToScene ?? "unknown"), new AnalyticsParam("from", evt.FromScene ?? string.Empty));
    }
}
