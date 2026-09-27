using System;
using System.Collections.Generic;
using UnityEngine;

namespace NinjaVillage.Systems.Kitchen
{
    public enum KitchenResult
    {
        Success,
        NotBuilt,
        UnknownRecipe,
        RecipeLocked,
        NoFreeSlot,
        MissingIngredients,
        SlotEmpty,
        NotReady,
    }

    public enum MealToggle
    {
        Added,
        Removed,
        /// <summary>Both meal slots are taken; take one out first.</summary>
        Full,
        NotAMeal,
    }

    /// <summary>
    /// The Kitchen's rules as pure functions (EPIC 24 Phase 2), kept free of assets and the save so EditMode
    /// tests pin them down: how many pots cook at once, when a meal is done, and the two meal slots.
    /// </summary>
    public static class KitchenRules
    {
        /// <summary>Meals you can take into one battle.</summary>
        public const int MaxMeals = 2;

        /// <summary>Pots cooking at once: the Kitchen's "Cooking slots" effect, at least one once it's built.</summary>
        public static int Slots(int kitchenLevel, float effect) =>
            kitchenLevel <= 0 ? 0 : Mathf.Max(1, Mathf.FloorToInt(effect + 0.0001f));

        public static bool IsReady(DateTime now, long readyTicks) => now.Ticks >= readyTicks;

        public static float Progress(DateTime now, long startTicks, long readyTicks)
        {
            if (readyTicks <= startTicks) return 1f;
            return Mathf.Clamp01((now.Ticks - startTicks) / (float)(readyTicks - startTicks));
        }

        /// <summary>Adds or removes <paramref name="mealId"/> from the picked meals (in place).</summary>
        public static MealToggle Toggle(List<string> selected, string mealId)
        {
            if (selected == null || string.IsNullOrEmpty(mealId)) return MealToggle.NotAMeal;
            if (selected.Remove(mealId)) return MealToggle.Removed;
            if (selected.Count >= MaxMeals) return MealToggle.Full;
            selected.Add(mealId);
            return MealToggle.Added;
        }
    }
}
