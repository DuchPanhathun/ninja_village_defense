using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills.Behaviors;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    /// <summary>
    /// Evolution: Smoke + Teleport = <b>Invisible Assassin</b>. You spend most of the fight unseen, and
    /// strikes from the shadows are far more likely to be lethal criticals.
    /// </summary>
    [CreateAssetMenu(fileName = "Skill_InvisibleAssassin", menuName = "Ninja Village/Skills/Evolutions/Invisible Assassin")]
    public class InvisibleAssassinSkill : SkillDefinition
    {
        [SerializeField] private float invisibleSeconds = 4f;
        [SerializeField] private float cycleSeconds = 6f;
        [SerializeField] private float critChanceBonus = 0.2f;
        [SerializeField] private float critDamageBonus = 0.75f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
        {
            if (!stats.TryGetComponent<InvisibilityBehavior>(out var invisibility))
                invisibility = stats.gameObject.AddComponent<InvisibilityBehavior>();
            invisibility.Configure(invisibleSeconds, cycleSeconds); // replaces Smoke Bomb's shorter window
            stats.AddCritChanceBonus(critChanceBonus);
            stats.AddCritMultiplierBonus(critDamageBonus);
        }
    }
}
