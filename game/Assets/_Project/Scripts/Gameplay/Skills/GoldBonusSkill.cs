using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "Skill_GoldBonus", menuName = "Ninja Village/Skills/Gold Bonus")]
    public class GoldBonusSkill : SkillDefinition
    {
        [SerializeField] private float bonusPerLevel = 0.15f;
        public override void ApplyLevel(PlayerStats stats, int newLevel) => stats.AddGoldBonusMultiplier(bonusPerLevel);
    }
}
