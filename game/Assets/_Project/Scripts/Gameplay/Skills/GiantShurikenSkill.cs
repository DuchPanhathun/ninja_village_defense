using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    /// <summary>Bigger projectiles, bigger damage — scales both together so the "giant" theme actually reads visually.</summary>
    [CreateAssetMenu(fileName = "Skill_GiantShuriken", menuName = "Ninja Village/Skills/Giant Shuriken")]
    public class GiantShurikenSkill : SkillDefinition
    {
        [SerializeField] private float sizePerLevel = 0.35f;
        [SerializeField] private float damagePerLevel = 0.25f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
        {
            stats.AddProjectileSizeMultiplier(sizePerLevel);
            stats.AddAttackDamageMultiplier(damagePerLevel);
        }
    }
}
