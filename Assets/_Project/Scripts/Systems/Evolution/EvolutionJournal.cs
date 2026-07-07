using System.Collections.Generic;
using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Systems.Evolution
{
    /// <summary>
    /// Persists which evolutions the player has discovered across the session.
    /// Add to SaveData when Cloud Save is implemented (EPIC 22).
    /// </summary>
    public class EvolutionJournal : MonoBehaviour
    {
        private readonly HashSet<string> _unlocked = new();

        public IReadOnlyCollection<string> Unlocked => _unlocked;

        private void OnEnable() => EventBus<EvolutionUnlockedEvent>.Subscribe(OnEvolutionUnlocked);
        private void OnDisable() => EventBus<EvolutionUnlockedEvent>.Unsubscribe(OnEvolutionUnlocked);

        public bool IsUnlocked(EvolutionRecipe recipe) => _unlocked.Contains(recipe.name);

        private void OnEvolutionUnlocked(EvolutionUnlockedEvent evt)
        {
            _unlocked.Add(evt.Recipe.name);
        }
    }
}
