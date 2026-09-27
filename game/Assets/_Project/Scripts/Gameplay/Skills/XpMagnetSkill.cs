using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "Skill_XpMagnet", menuName = "Ninja Village/Skills/XP Magnet")]
    public class XpMagnetSkill : SkillDefinition
    {
        [SerializeField] private float bonusPerLevel = 0.5f;
        public override void ApplyLevel(PlayerStats stats, int newLevel) => stats.AddXpMagnetRadiusMultiplier(bonusPerLevel);
    }
}
