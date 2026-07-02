using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills.Behaviors;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    // ---------------------------------------------------------------------
    // Skills that either apply on-hit status effects or grant a runtime
    // behavior component to the player. Behavior components are added lazily
    // by ApplyLevel, so none of these need scene wiring — just create the
    // ScriptableObject asset and put it in the SkillManager pool.
    // ---------------------------------------------------------------------

    [CreateAssetMenu(fileName = "Skill_FireBlade", menuName = "Ninja Village/Skills/Fire Blade")]
    public class FireBladeSkill : SkillDefinition
    {
        [Tooltip("Burn damage-per-second added to every hit, per skill level.")]
        [SerializeField] private float burnDpsPerLevel = 3f;
        [SerializeField] private float burnDuration = 3f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
            => stats.AddBurnOnHit(burnDpsPerLevel, burnDuration);
    }

    [CreateAssetMenu(fileName = "Skill_PoisonKunai", menuName = "Ninja Village/Skills/Poison Kunai")]
    public class PoisonKunaiSkill : SkillDefinition
    {
        [SerializeField] private float poisonDpsPerLevel = 2f;
        [SerializeField] private float poisonDuration = 5f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
            => stats.AddPoisonOnHit(poisonDpsPerLevel, poisonDuration);
    }

    [CreateAssetMenu(fileName = "Skill_LightningStrike", menuName = "Ninja Village/Skills/Lightning Strike")]
    public class LightningStrikeSkill : SkillDefinition
    {
        [SerializeField] private float baseDamage = 15f;
        [SerializeField] private float damagePerLevel = 10f;
        [SerializeField] private float baseInterval = 3f;
        [SerializeField] private float intervalReductionPerLevel = 0.3f;
        [SerializeField] private float radius = 7f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
        {
            var behavior = stats.GetComponent<LightningStrikeBehavior>();
            if (behavior == null) behavior = stats.gameObject.AddComponent<LightningStrikeBehavior>();

            float damage = baseDamage + damagePerLevel * (newLevel - 1);
            float interval = Mathf.Max(0.5f, baseInterval - intervalReductionPerLevel * (newLevel - 1));
            behavior.Configure(damage, interval, radius);
        }
    }

    [CreateAssetMenu(fileName = "Skill_ExplosiveBomb", menuName = "Ninja Village/Skills/Explosive Bomb")]
    public class ExplosiveBombSkill : SkillDefinition
    {
        [SerializeField] private float baseDamage = 25f;
        [SerializeField] private float damagePerLevel = 15f;
        [SerializeField] private float baseInterval = 5f;
        [SerializeField] private float intervalReductionPerLevel = 0.5f;
        [SerializeField] private float blastRadius = 2.5f;
        [SerializeField] private float throwRadius = 4f;

        public override void ApplyLevel(PlayerStats stats, int newLevel)
        {
            var behavior = stats.GetComponent<ExplosiveBombBehavior>();
            if (behavior == null) behavior = stats.gameObject.AddComponent<ExplosiveBombBehavior>();

            float damage = baseDamage + damagePerLevel * (newLevel - 1);
            float interval = Mathf.Max(1f, baseInterval - intervalReductionPerLevel * (newLevel - 1));
            behavior.Configure(damage, interval, blastRadius, throwRadius);
        }
    }

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
