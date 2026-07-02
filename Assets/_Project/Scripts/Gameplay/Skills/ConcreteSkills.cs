using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Skills
{
    // ---------------------------------------------------------------------
    // Concrete, generically-implementable skills from the design doc's pool.
    // Skills that need dedicated systems not yet built (elemental DoT/status
    // effects, AoE explosions, shields, invisibility — Giant Shuriken, Fire
    // Blade, Poison Kunai, Lightning Strike, Explosive Bomb, Smoke Bomb,
    // Shield) are intentionally NOT here yet; they depend on the Element/
    // Status system (EPIC 7) landing first.
    // ---------------------------------------------------------------------

    [CreateAssetMenu(fileName = "Skill_AttackSpeed", menuName = "Ninja Village/Skills/Attack Speed")]
    public class AttackSpeedSkill : SkillDefinition
    {
        [SerializeField] private float bonusPerLevel = 0.2f;
        public override void ApplyLevel(PlayerStats stats, int newLevel) => stats.AddAttackSpeedMultiplier(bonusPerLevel);
    }

    [CreateAssetMenu(fileName = "Skill_TripleThrow", menuName = "Ninja Village/Skills/Triple Throw")]
    public class TripleThrowSkill : SkillDefinition
    {
        [SerializeField] private int extraProjectilesPerLevel = 1;
        public override void ApplyLevel(PlayerStats stats, int newLevel) => stats.AddExtraProjectiles(extraProjectilesPerLevel);
    }

    [CreateAssetMenu(fileName = "Skill_DodgeChance", menuName = "Ninja Village/Skills/Dodge Chance")]
    public class DodgeChanceSkill : SkillDefinition
    {
        [SerializeField] private float bonusPerLevel = 0.05f;
        public override void ApplyLevel(PlayerStats stats, int newLevel) => stats.AddDodgeChance(bonusPerLevel);
    }

    [CreateAssetMenu(fileName = "Skill_AutoHeal", menuName = "Ninja Village/Skills/Auto Heal")]
    public class AutoHealSkill : SkillDefinition
    {
        [Tooltip("HP healed per second, added per level.")]
        [SerializeField] private float healPerSecondPerLevel = 0.5f;
        public override void ApplyLevel(PlayerStats stats, int newLevel) => stats.AddHealPerSecond(healPerSecondPerLevel);
    }

    [CreateAssetMenu(fileName = "Skill_MovementSpeed", menuName = "Ninja Village/Skills/Movement Speed")]
    public class MovementSpeedSkill : SkillDefinition
    {
        [SerializeField] private float bonusPerLevel = 0.1f;
        public override void ApplyLevel(PlayerStats stats, int newLevel) => stats.AddMoveSpeedMultiplier(bonusPerLevel);
    }

    [CreateAssetMenu(fileName = "Skill_XpMagnet", menuName = "Ninja Village/Skills/XP Magnet")]
    public class XpMagnetSkill : SkillDefinition
    {
        [SerializeField] private float bonusPerLevel = 0.5f;
        public override void ApplyLevel(PlayerStats stats, int newLevel) => stats.AddXpMagnetRadiusMultiplier(bonusPerLevel);
    }

    [CreateAssetMenu(fileName = "Skill_GoldBonus", menuName = "Ninja Village/Skills/Gold Bonus")]
    public class GoldBonusSkill : SkillDefinition
    {
        [SerializeField] private float bonusPerLevel = 0.15f;
        public override void ApplyLevel(PlayerStats stats, int newLevel) => stats.AddGoldBonusMultiplier(bonusPerLevel);
    }

    [CreateAssetMenu(fileName = "Skill_LuckyDrop", menuName = "Ninja Village/Skills/Lucky Drop")]
    public class LuckyDropSkill : SkillDefinition
    {
        [SerializeField] private float bonusPerLevel = 0.1f;
        public override void ApplyLevel(PlayerStats stats, int newLevel) => stats.AddLuckyDropChanceBonus(bonusPerLevel);
    }
}
