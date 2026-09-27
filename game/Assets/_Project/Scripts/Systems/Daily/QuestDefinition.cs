using NinjaVillage.Core.ScriptableObjects;
using UnityEngine;

namespace NinjaVillage.Systems.Daily
{
    public enum QuestScope
    {
        Daily,
        Weekly
    }

    /// <summary>
    /// One daily or weekly quest in the pool (EPIC 19): "reach <see cref="Target"/> of
    /// <see cref="StatId"/>" (a <c>ProgressStatIds</c> value, e.g. enemies_killed) for a reward.
    /// Every day/week a few are rolled from the pool by weight. The description is the player-facing
    /// task text ("Defeat 150 demons").
    /// </summary>
    [CreateAssetMenu(fileName = "Quest_New", menuName = "Ninja Village/Daily/Quest")]
    public class QuestDefinition : DescriptiveScriptableObject
    {
        [SerializeField] private QuestScope scope = QuestScope.Daily;
        [SerializeField] private string statId = "enemies_killed";
        [SerializeField, Min(1)] private int target = 10;
        [SerializeField] private DailyReward reward = DailyReward.Coins(100);
        [SerializeField, Min(0f)] private float weight = 1f;

        public QuestScope Scope => scope;
        public string StatId => statId;
        public int Target => Mathf.Max(1, target);
        public DailyReward Reward => reward;
        public float Weight => weight;

        public string TaskText => string.IsNullOrEmpty(Description) ? DisplayName : Description;
    }
}
