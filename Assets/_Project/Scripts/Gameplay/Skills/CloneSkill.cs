using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills.Behaviors;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    /// <summary>
    /// "Shadow Clone": permanent clones orbit you and throw shadow kunai at the nearest demon — one
    /// clone per level. The clone ingredient of the Shadow Army evolution (Clone + Katana).
    /// </summary>
    [CreateAssetMenu(fileName = "Skill_Clone", menuName = "Ninja Village/Skills/Shadow Clone")]
    public class CloneSkill : SkillDefinition
    {
        [SerializeField] private float damageFraction = 0.35f;
        [SerializeField] private float damageFractionPerLevel = 0.05f;
        [SerializeField] private float orbitRadius = 1.6f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
        {
            if (!stats.TryGetComponent<CloneBehavior>(out var behavior))
                behavior = stats.gameObject.AddComponent<CloneBehavior>();
            behavior.Configure(newLevel, damageFraction + damageFractionPerLevel * (newLevel - 1), orbitRadius);
        }
    }
}
