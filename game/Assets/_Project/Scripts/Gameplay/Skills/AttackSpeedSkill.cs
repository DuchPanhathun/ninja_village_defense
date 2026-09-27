using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "Skill_AttackSpeed", menuName = "Ninja Village/Skills/Attack Speed")]
    public class AttackSpeedSkill : SkillDefinition
    {
        [SerializeField] private float bonusPerLevel = 0.2f;
        public override void ApplyLevel(PlayerStats stats, int newLevel) => stats.AddAttackSpeedMultiplier(bonusPerLevel);
    }
}
