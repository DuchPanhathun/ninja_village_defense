using System;
using System.Collections.Generic;
using NinjaVillage.Core.ScriptableObjects;
using UnityEngine;

namespace NinjaVillage.Systems.LiveOps
{
    /// <summary>One event mission: reach <see cref="target"/> of a ProgressStatIds stat during the event.</summary>
    [Serializable]
    public class EventMission
    {
        public string id;
        public string description;
        public string statId;
        [Min(1)] public int target = 1;
        public List<LiveOpsReward> rewards = new();
    }

    /// <summary>
    /// A seasonal event (EPIC 20 "Seasonal events", "Event missions"): a UTC time window, battle bonuses
    /// applied at run start (extra coins / XP), and a set of missions with exclusive rewards. Can be
    /// switched off remotely with the Remote Config key <c>event_&lt;id&gt;_enabled</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "Event_New", menuName = "Ninja Village/Live Ops/Seasonal Event")]
    public class EventDefinition : DescriptiveScriptableObject
    {
        [Tooltip("UTC start date, yyyy-MM-dd.")]
        [SerializeField] private string startDate = "2026-10-01";
        [SerializeField, Min(1)] private int durationDays = 7;
        [SerializeField] private Color themeColor = new(1f, 0.6f, 0.8f, 1f);

        [Header("Battle bonuses while active (additive: 0.25 = +25%)")]
        [SerializeField] private float coinBonus = 0.25f;
        [SerializeField] private float xpBonus;

        [SerializeField] private List<EventMission> missions = new();

        public DateTime? StartUtc => LiveOpsRules.ParseDate(startDate);
        public DateTime? EndUtc => StartUtc?.AddDays(durationDays);
        public int DurationDays => durationDays;
        public Color ThemeColor => themeColor;
        public float CoinBonus => coinBonus;
        public float XpBonus => xpBonus;
        public IReadOnlyList<EventMission> Missions => missions;

        public bool IsInWindow(DateTime nowUtc) => LiveOpsRules.IsWithin(nowUtc, StartUtc, durationDays);

        public EventMission GetMission(string missionId)
        {
            foreach (var m in missions) if (m != null && m.id == missionId) return m;
            return null;
        }

#if UNITY_EDITOR
        public void EditorSetMissions(IEnumerable<EventMission> newMissions)
        {
            missions = new List<EventMission>(newMissions);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
