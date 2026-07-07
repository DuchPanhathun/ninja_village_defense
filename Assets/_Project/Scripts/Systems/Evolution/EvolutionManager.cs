using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Gameplay.Player;
using NinjaVillage.Gameplay.Skills;
using UnityEngine;

namespace NinjaVillage.Systems.Evolution
{
    /// <summary>
    /// Checks all registered evolution recipes each time the player picks a skill.
    /// When all prerequisites for a recipe are satisfied and it hasn't been unlocked yet,
    /// grants the result skill immediately and broadcasts <see cref="EvolutionUnlockedEvent"/>.
    /// </summary>
    [RequireComponent(typeof(SkillManager))]
    public class EvolutionManager : MonoBehaviour
    {
        [SerializeField] private EvolutionRecipe[] recipes;

        private SkillManager _skillManager;
        private AutoAttackController _autoAttack;
        private EvolutionJournal _journal;

        private void Awake()
        {
            _skillManager = GetComponent<SkillManager>();
            _autoAttack = GetComponent<AutoAttackController>();
            _journal = GetComponent<EvolutionJournal>();
        }

        private void OnEnable() => EventBus<SkillLeveledEvent>.Subscribe(OnSkillLeveled);
        private void OnDisable() => EventBus<SkillLeveledEvent>.Unsubscribe(OnSkillLeveled);

        private void OnSkillLeveled(SkillLeveledEvent evt) => CheckAllRecipes();

        private void CheckAllRecipes()
        {
            if (recipes == null) return;

            foreach (var recipe in recipes)
            {
                if (recipe == null || recipe.ResultSkill == null) continue;
                if (_journal != null && _journal.IsUnlocked(recipe)) continue;
                if (!IsRecipeSatisfied(recipe)) continue;

                UnlockEvolution(recipe);
            }
        }

        private bool IsRecipeSatisfied(EvolutionRecipe recipe)
        {
            // All required skills must be owned at level >= 1.
            foreach (var required in recipe.RequiredSkills)
            {
                if (required == null) continue;
                if (!_skillManager.Levels.TryGetValue(required, out int level) || level < 1)
                    return false;
            }

            // If a weapon is required, the equipped weapon must match.
            if (recipe.RequiredWeapon != null)
            {
                if (_autoAttack == null || _autoAttack.Weapon == null) return false;
                if (_autoAttack.Weapon.Definition != recipe.RequiredWeapon) return false;
            }

            return true;
        }

        private void UnlockEvolution(EvolutionRecipe recipe)
        {
            _skillManager.SelectSkill(recipe.ResultSkill);
            EventBus<EvolutionUnlockedEvent>.Raise(new EvolutionUnlockedEvent(recipe));
        }
    }
}
