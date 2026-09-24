using System.Collections.Generic;
using NinjaVillage.Core.Data;
using NinjaVillage.Systems.Evolution;
using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Save;
using NinjaVillage.UI.Common;
using UnityEngine;

namespace NinjaVillage.UI.Collection
{
    /// <summary>
    /// The evolution collection (EPIC 7 "Collection UI", "Hidden Discovery System"): discovered
    /// recipes show their ingredients and result; undiscovered ones stay "???" with only a vague hint,
    /// so players find combinations themselves (goal.text "Players discover combinations over time").
    /// Opening the screen clears the "new discovery" badge.
    /// </summary>
    [SceneScreen(SceneNames.MainMenu)]
    public class CollectionScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Collection;
        protected override string Title => "Forbidden Techniques";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterBadge() =>
            ScreenBadges.Register(ScreenIds.Collection, () => SaveService.Data.Evolutions.UnseenRecipeIds.Count > 0);

        protected override void Populate(RectTransform content)
        {
            var catalog = CatalogLoader.Load<EvolutionCatalog>();
            if (catalog == null || catalog.All.Count == 0)
            {
                InfoText.text = string.Empty;
                UIBuilder.Text(content, "No evolutions found. Run Ninja Village → Generate Default Content.", UITheme.BodySize);
                return;
            }

            var unseen = SaveService.Data.Evolutions.UnseenRecipeIds;
            int discovered = 0;
            foreach (var recipe in catalog.All)
            {
                if (recipe == null) continue;
                bool known = EvolutionJournal.IsDiscovered(recipe);
                if (known) discovered++;
                AddCard(content, recipe, known, unseen.Contains(recipe.Id));
            }

            InfoText.text = $"{discovered} / {catalog.All.Count} discovered. Combine the right skills (and weapons) in battle to reveal the rest.";
        }

        private static void AddCard(RectTransform content, EvolutionRecipe recipe, bool known, bool isNew)
        {
            if (!known)
            {
                string hint = string.IsNullOrEmpty(recipe.Hint) ? "An undiscovered technique..." : recipe.Hint;
                // Undiscovered: the technique's icon as a black silhouette — you can see there's something to find.
                UIBuilder.ActionCard(content, "???", $"<i>{hint}</i>", out _, out _, UITheme.TextMuted,
                    UIIcons.Evolution(recipe.Id), new Color(0.34f, 0.25f, 0.2f, 1f));
                return;
            }

            var parts = new List<string>();
            foreach (var skill in recipe.RequiredSkills)
                if (skill != null) parts.Add(skill.DisplayName);
            if (recipe.RequiredWeapon != null)
                parts.Add(string.IsNullOrEmpty(recipe.RequiredWeapon.DisplayName) ? recipe.RequiredWeapon.Id : recipe.RequiredWeapon.DisplayName);

            string result = recipe.ResultSkill != null ? recipe.ResultSkill.DisplayName : recipe.DisplayName;
            string body = $"{string.Join(" + ", parts)}  →  <b>{result}</b>";
            if (recipe.ResultSkill != null && !string.IsNullOrEmpty(recipe.ResultSkill.Description))
                body += "\n" + recipe.ResultSkill.Description;

            UIBuilder.ActionCard(content, isNew ? $"{result}  (NEW!)" : result, body, out _, out _, isNew ? UITheme.Gold : new Color(0.8f, 0.6f, 1f),
                UIIcons.Evolution(recipe.Id));
        }

        protected override void OnShown()
        {
            base.OnShown();
            var unseen = SaveService.Data.Evolutions.UnseenRecipeIds;
            if (unseen.Count == 0) return;
            unseen.Clear(); // they've now been seen; the NEW tags stay until the next visit
            SaveService.MarkDirty();
        }
    }
}
