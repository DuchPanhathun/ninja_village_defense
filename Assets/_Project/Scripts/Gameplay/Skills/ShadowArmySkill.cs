using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills.Behaviors;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    /// <summary>
    /// Evolution: Clone + Katana = <b>Shadow Army</b>. A permanent squad of shadow samurai circles you,
    /// each dashing in to slash nearby demons with a katana arc.
    /// </summary>
    [CreateAssetMenu(fileName = "Skill_ShadowArmy", menuName = "Ninja Village/Skills/Evolutions/Shadow Army")]
    public class ShadowArmySkill : SkillDefinition
    {
        [SerializeField] private int soldiers = 4;
        [SerializeField] private float damage = 28f;
        [SerializeField] private float attackInterval = 0.9f;
        [SerializeField] private float reach = 2.2f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
        {
            if (!stats.TryGetComponent<ShadowArmyBehavior>(out var behavior))
                behavior = stats.gameObject.AddComponent<ShadowArmyBehavior>();
            behavior.Configure(soldiers, damage * newLevel, attackInterval, reach);
        }
    }
}
