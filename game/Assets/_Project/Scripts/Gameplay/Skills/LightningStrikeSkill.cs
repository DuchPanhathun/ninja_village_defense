using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills.Behaviors;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "Skill_LightningStrike", menuName = "Ninja Village/Skills/Lightning Strike")]
    public class LightningStrikeSkill : SkillDefinition
    {
        [SerializeField] private float baseDamage = 15f;
        [SerializeField] private float damagePerLevel = 10f;
        [SerializeField] private float baseInterval = 3f;
        [SerializeField] private float intervalReductionPerLevel = 0.3f;
        [SerializeField] private float radius = 7f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
        {
            var behavior = stats.GetComponent<LightningStrikeBehavior>();
            if (behavior == null) behavior = stats.gameObject.AddComponent<LightningStrikeBehavior>();

            float damage = baseDamage + damagePerLevel * (newLevel - 1);
            float interval = Mathf.Max(0.5f, baseInterval - intervalReductionPerLevel * (newLevel - 1));
            behavior.Configure(damage, interval, radius);
        }
    }
}
