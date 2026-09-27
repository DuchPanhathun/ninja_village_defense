using System.Collections.Generic;
using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Data;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Daily
{
    /// <summary>Raised whenever login/quest/achievement state changes (progress, completion, claim, re-roll) — screens and badges refresh on it.</summary>
    public readonly struct DailyContentChangedEvent : IGameEvent { }

    /// <summary>Daily login rewards (EPIC 19 "Daily login"). Rules in <see cref="DailyRules"/>.</summary>
    public static class DailyLoginService
    {
        public static LoginCalendar Calendar => CatalogLoader.Load<LoginCalendar>();

        private static DailySaveData Data
        {
            get
            {
                var daily = SaveService.Data.Daily;
                daily.EnsureInitialized();
                return daily;
            }
        }

        public static bool CanClaimToday => DailyRules.CanClaimLogin(Data.LastLoginClaimDay, GameClock.Today);
        public static int CalendarLength => Calendar != null ? Calendar.Length : 7;
        public static int NextDayIndex => DailyRules.CalendarIndex(Data.TotalLoginClaims, CalendarLength);
        public static int Streak => Data.LoginStreak;
        public static int TotalClaims => Data.TotalLoginClaims;

        /// <summary>
        /// Calendar days shown as claimed in the current 7-day loop. Right after claiming the last day
        /// the loop wraps to 0, but the full week should still read as claimed until the next claim.
        /// </summary>
        public static int ClaimedThisCycle
        {
            get
            {
                int next = NextDayIndex;
                if (!CanClaimToday && next == 0 && Data.TotalLoginClaims > 0) return CalendarLength;
                return next;
            }
        }
        public static int BestStreak => Data.BestLoginStreak;

        /// <summary>The streak that claiming today would produce (for the "Day N streak" label).</summary>
        public static int StreakIfClaimedToday => DailyRules.NextStreak(Data.LastLoginClaimDay, Data.LoginStreak, GameClock.Today);

        public static DailyReward RewardAt(int dayIndex) => Calendar != null ? Calendar.RewardAt(dayIndex) : DailyReward.Coins(100);

        public static bool TryClaim(out DailyReward reward)
        {
            reward = default;
            if (!CanClaimToday) return false;

            var data = Data;
            int today = GameClock.Today;
            reward = RewardAt(NextDayIndex);

            data.LoginStreak = DailyRules.NextStreak(data.LastLoginClaimDay, data.LoginStreak, today);
            if (data.LoginStreak > data.BestLoginStreak) data.BestLoginStreak = data.LoginStreak;
            data.LastLoginClaimDay = today;
            data.TotalLoginClaims++;

            reward.Grant("daily_login");
            SaveService.SaveNow();
            Sfx.Play(AudioCueIds.RewardClaim);
            Progress.Report(ProgressStatIds.DailyLoginClaimed);
            EventBus<DailyContentChangedEvent>.Raise(new DailyContentChangedEvent());
            return true;
        }
    }

    /// <summary>
    /// Daily/weekly quests and achievements (EPIC 19): rolls today's and this week's quests
    /// deterministically, exposes their progress, and pays rewards on claim. Progress itself is fed
    /// by <see cref="QuestTracker"/>.
    /// </summary>
    public static class QuestService
    {
        public const int DailyQuestCount = 3;
        public const int WeeklyQuestCount = 3;
        private const int DailySalt = 1;
        private const int WeeklySalt = 2;

        public static QuestCatalog Quests => CatalogLoader.Load<QuestCatalog>();
        public static AchievementCatalog Achievements => CatalogLoader.Load<AchievementCatalog>();

        private static DailySaveData Data
        {
            get
            {
                var daily = SaveService.Data.Daily;
                daily.EnsureInitialized();
                return daily;
            }
        }

        private static readonly List<(string, float)> PoolBuffer = new();
        private static readonly List<string> RollBuffer = new();

        /// <summary>Re-rolls daily/weekly quests when the day/week changed. Returns true if anything was rolled.</summary>
        public static bool EnsureRolled()
        {
            var data = Data;
            bool changed = false;
            int today = GameClock.Today;
            int week = GameClock.ThisWeek;

            if (data.DailyQuestsDay != today)
            {
                Roll(QuestScope.Daily, today, DailySalt, DailyQuestCount, data.DailyQuests);
                data.DailyQuestsDay = today;
                changed = true;
            }
            if (data.WeeklyQuestsWeek != week)
            {
                Roll(QuestScope.Weekly, week, WeeklySalt, WeeklyQuestCount, data.WeeklyQuests);
                data.WeeklyQuestsWeek = week;
                changed = true;
            }

            if (changed)
            {
                SaveService.MarkDirty();
                EventBus<DailyContentChangedEvent>.Raise(new DailyContentChangedEvent());
            }
            return changed;
        }

        private static void Roll(QuestScope scope, int periodIndex, int salt, int count, List<QuestProgress> into)
        {
            into.Clear();
            var catalog = Quests;
            if (catalog == null) return;

            PoolBuffer.Clear();
            foreach (var quest in catalog.All)
                if (quest != null && quest.Scope == scope) PoolBuffer.Add((quest.Id, quest.Weight));

            int seed = DailyRules.SeedFor(periodIndex, SaveService.Data.Profile.PlayerId, salt);
            DailyRules.RollQuests(PoolBuffer, count, seed, RollBuffer);
            foreach (var id in RollBuffer)
                into.Add(new QuestProgress { Id = id });
        }

        public static QuestDefinition GetQuest(string id)
        {
            var catalog = Quests;
            return catalog != null ? catalog.Get(id) : null;
        }

        public static IReadOnlyList<QuestProgress> Daily => Data.DailyQuests;
        public static IReadOnlyList<QuestProgress> Weekly => Data.WeeklyQuests;

        public static bool TryClaimQuest(QuestProgress progress)
        {
            if (progress == null || !progress.Completed || progress.Claimed) return false;
            var quest = GetQuest(progress.Id);
            if (quest == null) return false;

            progress.Claimed = true;
            quest.Reward.Grant(quest.Id);
            SaveService.SaveNow();
            Sfx.Play(AudioCueIds.RewardClaim);
            EventBus<DailyContentChangedEvent>.Raise(new DailyContentChangedEvent());
            return true;
        }

        public static QuestProgress AchievementProgress(AchievementDefinition achievement) =>
            DailySaveData.GetOrAdd(Data.Achievements, achievement.Id);

        public static int ClaimableTiers(AchievementDefinition achievement)
        {
            var progress = AchievementProgress(achievement);
            return DailyRules.ClaimableTiers(progress.Progress, progress.ClaimedTiers, achievement.TierTargets);
        }

        /// <summary>Claims the next reached-but-unclaimed tier.</summary>
        public static bool TryClaimAchievementTier(AchievementDefinition achievement, out DailyReward reward)
        {
            reward = default;
            if (achievement == null || ClaimableTiers(achievement) <= 0) return false;

            var progress = AchievementProgress(achievement);
            reward = achievement.RewardForTier(progress.ClaimedTiers);
            progress.ClaimedTiers++;
            reward.Grant(achievement.Id);
            SaveService.SaveNow();
            Sfx.Play(AudioCueIds.RewardClaim);
            EventBus<DailyContentChangedEvent>.Raise(new DailyContentChangedEvent());
            return true;
        }

        public static bool HasClaimableQuest
        {
            get
            {
                EnsureRolled();
                foreach (var q in Data.DailyQuests) if (q != null && q.Completed && !q.Claimed) return true;
                foreach (var q in Data.WeeklyQuests) if (q != null && q.Completed && !q.Claimed) return true;
                return false;
            }
        }

        public static bool HasClaimableAchievement
        {
            get
            {
                var catalog = Achievements;
                if (catalog == null) return false;
                foreach (var achievement in catalog.All)
                    if (achievement != null && ClaimableTiers(achievement) > 0) return true;
                return false;
            }
        }
    }
}
