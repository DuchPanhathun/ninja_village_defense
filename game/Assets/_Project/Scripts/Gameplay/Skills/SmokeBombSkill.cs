using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills.Behaviors;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "Skill_SmokeBomb", menuName = "Ninja Village/Skills/Smoke Bomb")]
    public class SmokeBombSkill : SkillDefinition
    {
        [Tooltip("Seconds invisible per cycle.")]
        [SerializeField] private float activeDurationPerLevel = 1.5f;
        [Tooltip("Full cycle length (active + cooldown). Active portion = activeDurationPerLevel * level.")]
        [SerializeField] private float cycleDuration = 6f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
        {
            var behavior = stats.GetComponent<InvisibilityBehavior>();
            if (behavior == null) behavior = stats.gameObject.AddComponent<InvisibilityBehavior>();

            float activeDuration = activeDurationPerLevel * newLevel;
            behavior.Configure(activeDuration, cycleDuration);
        }
    }
}
