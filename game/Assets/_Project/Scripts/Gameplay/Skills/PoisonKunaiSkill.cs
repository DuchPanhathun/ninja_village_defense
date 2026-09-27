using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "Skill_PoisonKunai", menuName = "Ninja Village/Skills/Poison Kunai")]
    public class PoisonKunaiSkill : SkillDefinition
    {
        [SerializeField] private float poisonDpsPerLevel = 2f;
        [SerializeField] private float poisonDuration = 5f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
            => stats.AddPoisonOnHit(poisonDpsPerLevel, poisonDuration);
    }
}
