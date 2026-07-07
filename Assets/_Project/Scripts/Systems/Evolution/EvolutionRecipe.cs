using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Gameplay.Skills;
using NinjaVillage.Gameplay.Weapons;
using UnityEngine;

namespace NinjaVillage.Systems.Evolution
{
    /// <summary>
    /// Defines one evolution combination. The EvolutionManager checks all registered
    /// recipes each time the player picks a skill or changes weapon. When all
    /// prerequisites are met, the result skill is granted and the unlock is journaled.
    ///
    /// Recipes reference skills/weapons directly (no element-tag matching) so they
    /// compose cleanly with the existing SkillManager / AutoAttackController.
    /// </summary>
    [CreateAssetMenu(fileName = "Evolution_New", menuName = "Ninja Village/Evolution Recipe")]
    public class EvolutionRecipe : DescriptiveScriptableObject
    {
        [Header("Prerequisites")]
        [Tooltip("All of these skills must be owned (level >= 1) to trigger the evolution.")]
        [SerializeField] private SkillDefinition[] requiredSkills = {};

        [Tooltip("Optional: a specific weapon must be equipped.")]
        [SerializeField] private WeaponDefinition requiredWeapon;

        [Header("Result")]
        [Tooltip("The evolved skill granted when prerequisites are satisfied.")]
        [SerializeField] private SkillDefinition resultSkill;

        public SkillDefinition[] RequiredSkills => requiredSkills;
        public WeaponDefinition RequiredWeapon => requiredWeapon;
        public SkillDefinition ResultSkill => resultSkill;
    }
}
