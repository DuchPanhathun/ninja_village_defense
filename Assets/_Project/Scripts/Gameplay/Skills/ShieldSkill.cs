using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills.Behaviors;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "Skill_Shield", menuName = "Ninja Village/Skills/Shield")]
    public class ShieldSkill : SkillDefinition
    {
        [SerializeField] private float shieldPerTickPerLevel = 5f;
        [SerializeField] private float shieldCapPerLevel = 15f;
        [SerializeField] private float interval = 4f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
        {
            var behavior = stats.GetComponent<ShieldRegenBehavior>();
            if (behavior == null) behavior = stats.gameObject.AddComponent<ShieldRegenBehavior>();

            behavior.Configure(shieldPerTickPerLevel * newLevel, shieldCapPerLevel * newLevel, interval);
        }
    }
}
