using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "Skill_MovementSpeed", menuName = "Ninja Village/Skills/Movement Speed")]
    public class MovementSpeedSkill : SkillDefinition
    {
        [SerializeField] private float bonusPerLevel = 0.1f;
        public override void ApplyLevel(PlayerStats stats, int newLevel) => stats.AddMoveSpeedMultiplier(bonusPerLevel);
    }
}
