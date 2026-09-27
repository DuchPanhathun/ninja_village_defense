using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "Skill_FireBlade", menuName = "Ninja Village/Skills/Fire Blade")]
    public class FireBladeSkill : SkillDefinition
    {
        [Tooltip("Burn damage-per-second added to every hit, per skill level.")]
        [SerializeField] private float burnDpsPerLevel = 3f;
        [SerializeField] private float burnDuration = 3f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
            => stats.AddBurnOnHit(burnDpsPerLevel, burnDuration);
    }
}
