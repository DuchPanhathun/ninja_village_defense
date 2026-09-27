using System.Collections.Generic;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Evolution
{
    /// <summary>
    /// Tracks evolutions: which ones were granted this run (so a recipe fires once per run) and which
    /// the player has ever discovered — persisted in <see cref="EvolutionSaveData"/> for the Collection
    /// screen's hidden-discovery list (EPIC 7). New discoveries are also marked "unseen" for a badge
    /// and reported as <see cref="ProgressStatIds.EvolutionDiscovered"/>.
    /// </summary>
    public class EvolutionJournal : MonoBehaviour
    {
        private readonly HashSet<string> _unlockedThisRun = new();

        public IReadOnlyCollection<string> Unlocked => _unlockedThisRun;

        private void OnEnable() => EventBus<EvolutionUnlockedEvent>.Subscribe(OnEvolutionUnlocked);
        private void OnDisable() => EventBus<EvolutionUnlockedEvent>.Unsubscribe(OnEvolutionUnlocked);

        /// <summary>Granted during the current run.</summary>
        public bool IsUnlocked(EvolutionRecipe recipe) => recipe != null && _unlockedThisRun.Contains(Key(recipe));

        /// <summary>Ever discovered (any run).</summary>
        public static bool IsDiscovered(EvolutionRecipe recipe) =>
            recipe != null && SaveService.Data.Evolutions.DiscoveredRecipeIds.Contains(Key(recipe));

        private static string Key(EvolutionRecipe recipe) => string.IsNullOrEmpty(recipe.Id) ? recipe.name : recipe.Id;

        private void OnEvolutionUnlocked(EvolutionUnlockedEvent evt)
        {
            if (evt.Recipe == null) return;
            string key = Key(evt.Recipe);
            _unlockedThisRun.Add(key);

            var data = SaveService.Data.Evolutions;
            if (data.DiscoveredRecipeIds.Contains(key)) return;
            data.DiscoveredRecipeIds.Add(key);
            if (!data.UnseenRecipeIds.Contains(key)) data.UnseenRecipeIds.Add(key);
            SaveService.MarkDirty();
            Progress.Report(ProgressStatIds.EvolutionDiscovered, 1, key);
        }
    }
}
