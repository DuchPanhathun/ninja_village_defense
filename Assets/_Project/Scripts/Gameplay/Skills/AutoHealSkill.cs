using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "Skill_AutoHeal", menuName = "Ninja Village/Skills/Auto Heal")]
    public class AutoHealSkill : SkillDefinition
    {
        [Tooltip("HP healed per second, added per level.")]
        [SerializeField] private float healPerSecondPerLevel = 0.5f;
        public override void ApplyLevel(PlayerStats stats, int newLevel) => stats.AddHealPerSecond(healPerSecondPerLevel);
    }
}
