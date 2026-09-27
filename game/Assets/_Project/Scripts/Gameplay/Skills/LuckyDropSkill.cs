using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "Skill_LuckyDrop", menuName = "Ninja Village/Skills/Lucky Drop")]
    public class LuckyDropSkill : SkillDefinition
    {
        [SerializeField] private float bonusPerLevel = 0.1f;
        public override void ApplyLevel(PlayerStats stats, int newLevel) => stats.AddLuckyDropChanceBonus(bonusPerLevel);
    }
}
