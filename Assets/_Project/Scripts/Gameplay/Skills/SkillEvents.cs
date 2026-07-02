using NinjaVillage.Core.Events;

namespace NinjaVillage.Gameplay.Skills
{
    /// <summary>Raised after a level-up once 3 random eligible skills have been rolled — the Battle UI shows these as pick cards.</summary>
    public readonly struct SkillChoicesReadyEvent : IGameEvent
    {
        public readonly SkillDefinition[] Choices;
        public SkillChoicesReadyEvent(SkillDefinition[] choices) => Choices = choices;
    }

    /// <summary>Raised after the player picks a card and the effect has been applied.</summary>
    public readonly struct SkillLeveledEvent : IGameEvent
    {
        public readonly SkillDefinition Skill;
        public readonly int NewLevel;
        public SkillLeveledEvent(SkillDefinition skill, int newLevel)
        {
            Skill = skill;
            NewLevel = newLevel;
        }
    }
}
