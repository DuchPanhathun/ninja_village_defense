using System;
using System.Collections.Generic;
using NinjaVillage.Core.Events;

namespace NinjaVillage.Systems.Daily
{
    /// <summary>
    /// Daily-content rules with no Unity dependencies (EditMode-tested).
    ///
    /// Login calendar: one claim per GameClock day; the 7-day calendar advances one step per claim
    /// and never resets (missing a day never costs calendar progress — generous by design, so a
    /// missed day doesn't feel punishing). The streak counter does reset after a missed day. A clock
    /// that moved backwards (today &lt; last claim day) blocks claiming instead of granting twice.
    /// </summary>
    public static class DailyRules
    {
        // ---------------------------------------------------------------- login

        public static bool CanClaimLogin(int lastClaimDay, int today) => lastClaimDay < 0 || today > lastClaimDay;

        public static int NextStreak(int lastClaimDay, int currentStreak, int today) =>
            lastClaimDay >= 0 && today == lastClaimDay + 1 ? currentStreak + 1 : 1;

        public static int CalendarIndex(int totalClaims, int calendarLength) =>
            calendarLength <= 0 ? 0 : Math.Max(0, totalClaims) % calendarLength;

        // ---------------------------------------------------------------- quests

        /// <summary>Stats that track a best value (max) instead of a running total.</summary>
        public static bool IsMaxStat(string statId) =>
            statId == ProgressStatIds.WaveReached || statId == ProgressStatIds.SurvivedSeconds;

        /// <summary>
        /// Battle stats the <c>QuestTracker</c> derives from combat events. ProgressStatEvents carrying
        /// these ids are ignored so nothing is counted twice.
        /// </summary>
        public static bool IsCombatDerivedStat(string statId) =>
            statId == ProgressStatIds.EnemiesKilled || statId == ProgressStatIds.BossesKilled ||
            statId == ProgressStatIds.RunCompleted || statId == ProgressStatIds.RunVictory ||
            statId == ProgressStatIds.WaveReached || statId == ProgressStatIds.SurvivedSeconds ||
            statId == ProgressStatIds.UltimateUsed || statId == ProgressStatIds.SkillPicked;

        /// <summary>New progress after a stat report: summed, or the best value for max stats.</summary>
        public static int ApplyProgress(int current, int amount, bool isMax)
        {
            if (amount <= 0) return current;
            if (isMax) return Math.Max(current, amount);
            long sum = (long)current + amount;
            return sum > int.MaxValue ? int.MaxValue : (int)sum;
        }

        /// <summary>
        /// Picks <paramref name="count"/> distinct quest ids, weighted, deterministically from
        /// <paramref name="seed"/> — the same day/week and player always get the same quests.
        /// </summary>
        public static void RollQuests(IReadOnlyList<(string id, float weight)> pool, int count, int seed, List<string> results)
        {
            results.Clear();
            if (pool == null || count <= 0) return;

            var candidates = new List<(string id, float weight)>();
            foreach (var entry in pool)
                if (!string.IsNullOrEmpty(entry.id) && entry.weight > 0f) candidates.Add(entry);
            candidates.Sort((a, b) => string.CompareOrdinal(a.id, b.id)); // order-independent of the catalog

            var rng = new Random(seed);
            while (results.Count < count && candidates.Count > 0)
            {
                float total = 0f;
                foreach (var c in candidates) total += c.weight;
                double roll = rng.NextDouble() * total;
                int pick = candidates.Count - 1;
                for (int i = 0; i < candidates.Count; i++)
                {
                    roll -= candidates[i].weight;
                    if (roll <= 0) { pick = i; break; }
                }
                results.Add(candidates[pick].id);
                candidates.RemoveAt(pick);
            }
        }

        public static int SeedFor(int periodIndex, string playerId, int salt)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + periodIndex;
                hash = hash * 31 + salt;
                if (!string.IsNullOrEmpty(playerId))
                    foreach (char c in playerId) hash = hash * 31 + c;
                return hash;
            }
        }

        // ---------------------------------------------------------------- achievements

        /// <summary>How many tiers <paramref name="progress"/> has reached.</summary>
        public static int ReachedTiers(int progress, IReadOnlyList<int> tierTargets)
        {
            if (tierTargets == null) return 0;
            int reached = 0;
            for (int i = 0; i < tierTargets.Count; i++)
            {
                if (progress >= tierTargets[i]) reached = i + 1;
                else break;
            }
            return reached;
        }

        /// <summary>Tiers reached but not yet claimed.</summary>
        public static int ClaimableTiers(int progress, int claimedTiers, IReadOnlyList<int> tierTargets) =>
            Math.Max(0, ReachedTiers(progress, tierTargets) - Math.Max(0, claimedTiers));
    }
}
