using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills.Behaviors;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    /// <summary>
    /// Evolution: Lightning + Kunai = <b>Thunder Kunai</b>. Your throws crackle with power: attacks are
    /// faster, and chain lightning arcs from demon to demon, stunning each one it hits.
    /// </summary>
    [CreateAssetMenu(fileName = "Skill_ThunderKunai", menuName = "Ninja Village/Skills/Evolutions/Thunder Kunai")]
    public class ThunderKunaiSkill : SkillDefinition
    {
        [SerializeField] private float attackSpeedBonus = 0.25f;
        [SerializeField] private float damage = 30f;
        [SerializeField] private float interval = 1.2f;
        [SerializeField] private int chains = 5;
        [SerializeField] private float chainRadius = 3.5f;
        [SerializeField] private float stunSeconds = 0.35f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
        {
            stats.AddAttackSpeedMultiplier(attackSpeedBonus);
            if (!stats.TryGetComponent<ThunderKunaiBehavior>(out var behavior))
                behavior = stats.gameObject.AddComponent<ThunderKunaiBehavior>();
            behavior.Configure(damage * newLevel, interval, chains, chainRadius, stunSeconds);
        }
    }
}
