using NinjaVillage.Core.Data;
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
            if (_journal == null) _journal = gameObject.AddComponent<EvolutionJournal>();

            // No recipes assigned in the scene → use every recipe in the catalog.
            if (recipes == null || recipes.Length == 0)
            {
                var catalog = CatalogLoader.Load<EvolutionCatalog>();
                if (catalog != null)
                {
                    var all = new System.Collections.Generic.List<EvolutionRecipe>();
                    foreach (var recipe in catalog.All) if (recipe != null) all.Add(recipe);
                    recipes = all.ToArray();
                }
            }
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
                if (_granted.Contains(recipe)) continue;
                if (_journal != null && _journal.IsUnlocked(recipe)) continue;
                if (!IsRecipeSatisfied(recipe)) continue;

                UnlockEvolution(recipe);
            }
        }

        private bool IsRecipeSatisfied(EvolutionRecipe recipe)
        {
            string equipped = _autoAttack != null && _autoAttack.Weapon != null ? _autoAttack.Weapon.Definition.Id : null;
            var required = new System.Collections.Generic.List<string>();
            foreach (var skill in recipe.RequiredSkills) if (skill != null) required.Add(skill.Id);
            return EvolutionRules.IsSatisfied(required, SkillLevelById,
                recipe.RequiredWeapon != null ? recipe.RequiredWeapon.Id : null, equipped);
        }

        private int SkillLevelById(string skillId)
        {
            foreach (var pair in _skillManager.Levels)
                if (pair.Key != null && pair.Key.Id == skillId) return pair.Value;
            return 0;
        }

        // Marked BEFORE granting: SelectSkill raises SkillLeveledEvent, which re-runs CheckAllRecipes
        // synchronously — without this guard the same recipe would re-fire until the stack overflowed.
        private readonly System.Collections.Generic.HashSet<EvolutionRecipe> _granted = new();

        private void UnlockEvolution(EvolutionRecipe recipe)
        {
            _granted.Add(recipe);
            _skillManager.SelectSkill(recipe.ResultSkill);
            EventBus<EvolutionUnlockedEvent>.Raise(new EvolutionUnlockedEvent(recipe));
        }
    }
}
