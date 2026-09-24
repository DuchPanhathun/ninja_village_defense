using System.Collections.Generic;
using UnityEngine;

namespace NinjaVillage.Systems.Daily
{
    /// <summary>
    /// The 7-day login reward calendar (EPIC 19 "Daily login"), looping forever. Lives at
    /// <c>Resources/Catalogs/LoginCalendar.asset</c>. Day 7 is the big gem reward.
    /// </summary>
    [CreateAssetMenu(fileName = "LoginCalendar", menuName = "Ninja Village/Daily/Login Calendar")]
    public class LoginCalendar : ScriptableObject
    {
        [SerializeField] private List<DailyReward> days = new();

        public IReadOnlyList<DailyReward> Days => days;
        public int Length => days.Count;

        public DailyReward RewardAt(int index) => days.Count == 0 ? DailyReward.Coins(100) : days[Mathf.Clamp(index, 0, days.Count - 1)];

#if UNITY_EDITOR
        public void EditorSetDays(IEnumerable<DailyReward> rewards)
        {
            days = new List<DailyReward>(rewards);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
