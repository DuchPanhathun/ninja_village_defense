using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "Skill_DodgeChance", menuName = "Ninja Village/Skills/Dodge Chance")]
    public class DodgeChanceSkill : SkillDefinition
    {
        [SerializeField] private float bonusPerLevel = 0.05f;
        public override void ApplyLevel(PlayerStats stats, int newLevel) => stats.AddDodgeChance(bonusPerLevel);
    }
}
