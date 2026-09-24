using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills.Behaviors;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    /// <summary>
    /// "Blink": when you're hit, you vanish and reappear a few steps away from the crowd, briefly
    /// untouchable — on a cooldown that shortens per level. The teleport ingredient of the Invisible
    /// Assassin evolution (Smoke + Teleport).
    /// </summary>
    [CreateAssetMenu(fileName = "Skill_Teleport", menuName = "Ninja Village/Skills/Blink (Teleport)")]
    public class TeleportSkill : SkillDefinition
    {
        [SerializeField] private float baseCooldown = 8f;
        [SerializeField] private float cooldownReductionPerLevel = 1f;
        [SerializeField] private float blinkDistance = 4f;
        [SerializeField] private float invulnerabilitySeconds = 0.6f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
        {
            if (!stats.TryGetComponent<TeleportBehavior>(out var behavior))
                behavior = stats.gameObject.AddComponent<TeleportBehavior>();
            float cooldown = Mathf.Max(2f, baseCooldown - cooldownReductionPerLevel * (newLevel - 1));
            behavior.Configure(cooldown, blinkDistance, invulnerabilitySeconds);
        }
    }
}
