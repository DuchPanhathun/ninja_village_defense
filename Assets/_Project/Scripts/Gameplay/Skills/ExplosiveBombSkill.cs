using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills.Behaviors;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "Skill_ExplosiveBomb", menuName = "Ninja Village/Skills/Explosive Bomb")]
    public class ExplosiveBombSkill : SkillDefinition
    {
        [SerializeField] private float baseDamage = 25f;
        [SerializeField] private float damagePerLevel = 15f;
        [SerializeField] private float baseInterval = 5f;
        [SerializeField] private float intervalReductionPerLevel = 0.5f;
        [SerializeField] private float blastRadius = 2.5f;
        [SerializeField] private float throwRadius = 4f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
        {
            var behavior = stats.GetComponent<ExplosiveBombBehavior>();
            if (behavior == null) behavior = stats.gameObject.AddComponent<ExplosiveBombBehavior>();

            float damage = baseDamage + damagePerLevel * (newLevel - 1);
            float interval = Mathf.Max(1f, baseInterval - intervalReductionPerLevel * (newLevel - 1));
            behavior.Configure(damage, interval, blastRadius, throwRadius);
        }
    }
}
