using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    /// <summary>
    /// "Gale Step" (element Wind): the wind ingredient of the Firestorm evolution (Fire + Wind). Each
    /// level makes you faster and your throws quicker.
    /// </summary>
    [CreateAssetMenu(fileName = "Skill_Wind", menuName = "Ninja Village/Skills/Gale Step (Wind)")]
    public class WindSkill : SkillDefinition
    {
        [SerializeField] private float moveSpeedPerLevel = 0.06f;
        [SerializeField] private float attackSpeedPerLevel = 0.05f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
        {
            stats.AddMoveSpeedMultiplier(moveSpeedPerLevel);
            stats.AddAttackSpeedMultiplier(attackSpeedPerLevel);
        }
    }
}
