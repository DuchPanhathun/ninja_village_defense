using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>Progress on one quest / achievement / event mission.</summary>
    [Serializable]
    public class QuestProgress
    {
        public string Id;
        public int Progress;
        public bool Completed;
        public bool Claimed;
        /// <summary>Achievements only: how many tiers have been claimed (0..tier count).</summary>
        public int ClaimedTiers;
    }

    /// <summary>
    /// Daily login, daily/weekly quests and achievements (EPIC 19). Owned by the
    /// Daily Content system. Day/week indices come from <c>GameClock</c>.
    ///
    /// Login rules (see <c>LoginCalendarRules</c>): the 7-day calendar advances one day per
    /// claim and never resets — missing a day does not cost the player their calendar
    /// position. <see cref="LoginStreak"/> is the separate consecutive-days counter, which
    /// DOES reset to 1 after a missed day (it drives the streak label and achievement).
    /// </summary>
    [Serializable]
    public class DailySaveData
    {
        /// <summary>GameClock day index of the last login-reward claim; -1 = never claimed.</summary>
        public int LastLoginClaimDay = -1;
        /// <summary>Consecutive days with a claim, as of <see cref="LastLoginClaimDay"/>.</summary>
        public int LoginStreak;
        /// <summary>Longest streak ever reached.</summary>
        public int BestLoginStreak;
        /// <summary>Lifetime claims; <c>TotalLoginClaims % calendarLength</c> is the next calendar day.</summary>
        public int TotalLoginClaims;

        public int DailyQuestsDay = -1;
        public List<QuestProgress> DailyQuests = new();

        public int WeeklyQuestsWeek = -1;
        public List<QuestProgress> WeeklyQuests = new();

        public List<QuestProgress> Achievements = new();

        /// <summary>Fills lists that a hand-edited or cloud-merged save left null.</summary>
        public void EnsureInitialized()
        {
            DailyQuests ??= new List<QuestProgress>();
            WeeklyQuests ??= new List<QuestProgress>();
            Achievements ??= new List<QuestProgress>();
        }

        public static QuestProgress Find(List<QuestProgress> list, string id)
        {
            if (list == null || string.IsNullOrEmpty(id)) return null;
            foreach (var entry in list)
                if (entry != null && entry.Id == id) return entry;
            return null;
        }

        /// <summary>Returns the entry for <paramref name="id"/>, adding a fresh one if missing.</summary>
        public static QuestProgress GetOrAdd(List<QuestProgress> list, string id)
        {
            var entry = Find(list, id);
            if (entry != null) return entry;
            entry = new QuestProgress { Id = id };
            list.Add(entry);
            return entry;
        }
    }
}
