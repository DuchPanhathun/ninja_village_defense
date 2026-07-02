using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    /// <summary>
    /// Owns the player's skill levels. On every level-up, rolls 3 random eligible
    /// skills (level &lt; max) from the pool and waits for the Battle UI to call
    /// <see cref="SelectSkill"/> with the player's pick.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class SkillManager : MonoBehaviour
    {
        [SerializeField] private SkillDefinition[] skillPool;
        [SerializeField] private int choicesPerLevelUp = 3;

        private PlayerStats _stats;
        private readonly Dictionary<SkillDefinition, int> _levels = new();
        private readonly List<SkillDefinition> _eligibleBuffer = new();

        public IReadOnlyDictionary<SkillDefinition, int> Levels => _levels;

        private void Awake() => _stats = GetComponent<PlayerStats>();

        private void OnEnable() => EventBus<LevelUpEvent>.Subscribe(OnLevelUp);
        private void OnDisable() => EventBus<LevelUpEvent>.Unsubscribe(OnLevelUp);

        private void OnLevelUp(LevelUpEvent evt) => RollChoices();

        private void RollChoices()
        {
            _eligibleBuffer.Clear();
            foreach (var skill in skillPool)
            {
                int currentLevel = _levels.GetValueOrDefault(skill, 0);
                if (currentLevel < skill.MaxLevel)
                    _eligibleBuffer.Add(skill);
            }

            if (_eligibleBuffer.Count == 0) return;

            var choices = new SkillDefinition[Mathf.Min(choicesPerLevelUp, _eligibleBuffer.Count)];
            for (int i = 0; i < choices.Length; i++)
            {
                int index = Random.Range(0, _eligibleBuffer.Count);
                choices[i] = _eligibleBuffer[index];
                _eligibleBuffer.RemoveAt(index);
            }

            EventBus<SkillChoicesReadyEvent>.Raise(new SkillChoicesReadyEvent(choices));
        }

        /// <summary>Called by the Battle UI when the player taps one of the 3 offered cards.</summary>
        public void SelectSkill(SkillDefinition skill)
        {
            int newLevel = _levels.GetValueOrDefault(skill, 0) + 1;
            _levels[skill] = newLevel;
            skill.ApplyLevel(_stats, newLevel);

            EventBus<SkillLeveledEvent>.Raise(new SkillLeveledEvent(skill, newLevel));
        }
    }
}
