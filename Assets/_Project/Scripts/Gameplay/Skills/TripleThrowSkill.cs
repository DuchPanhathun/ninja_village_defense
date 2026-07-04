using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "Skill_TripleThrow", menuName = "Ninja Village/Skills/Triple Throw")]
    public class TripleThrowSkill : SkillDefinition
    {
        [SerializeField] private int extraProjectilesPerLevel = 1;
        public override void ApplyLevel(PlayerStats stats, int newLevel) => stats.AddExtraProjectiles(extraProjectilesPerLevel);
    }
}
