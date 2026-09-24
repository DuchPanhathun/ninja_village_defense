using System;
using System.Collections.Generic;
using NinjaVillage.Core.Data;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;

namespace NinjaVillage.Systems.Kitchen
{
    /// <summary>Raised whenever something starts cooking, is collected, or the picked meals change.</summary>
    public readonly struct KitchenChangedEvent : IGameEvent { }

    /// <summary>
    /// The Kitchen (EPIC 24 Phase 2), the link from the village back to battle: cook storehouse ingredients
    /// into meals (real time on <see cref="GameClock"/>, one pot per cooking slot), collect them into the
    /// storehouse, and pick up to two meals to eat when the next battle starts — <see cref="MealRunModifier"/>
    /// eats one of each and applies its boost for that run. State in <c>SaveService.Data.Kitchen</c>;
    /// rules in <see cref="KitchenRules"/>.
    /// </summary>
    public static class KitchenService
    {
        public static RecipeCatalog Catalog => CatalogLoader.Load<RecipeCatalog>();

        private static KitchenSaveData Data => SaveService.Data.Kitchen;

        public static int KitchenLevel => VillageService.GetLevel(BuildingIds.Kitchen);
        public static bool IsBuilt => KitchenLevel > 0;
        public static int SlotCount => KitchenRules.Slots(KitchenLevel, VillageService.GetEffect(BuildingIds.Kitchen, 1f));

        /// <summary>The meals eaten at the start of the current (or last) battle, for the battle HUD.</summary>
        public static IReadOnlyList<GoodsDefinition> MealsThisRun => _mealsThisRun;
        private static readonly List<GoodsDefinition> _mealsThisRun = new();

        public static RecipeDefinition GetRecipe(string id)
        {
            var catalog = Catalog;
            return catalog != null ? catalog.Get(id) : null;
        }

        /// <summary>Recipes in unlock order (a new list per call).</summary>
        public static List<RecipeDefinition> GetRecipes()
        {
            var list = new List<RecipeDefinition>();
            var catalog = Catalog;
            if (catalog == null) return list;
            foreach (var recipe in catalog.All)
                if (recipe != null) list.Add(recipe);
            list.Sort((a, b) => a.RequiredKitchenLevel != b.RequiredKitchenLevel
                ? a.RequiredKitchenLevel.CompareTo(b.RequiredKitchenLevel)
                : a.CookSeconds.CompareTo(b.CookSeconds));
            return list;
        }

        public static bool IsUnlocked(RecipeDefinition recipe) => recipe != null && KitchenLevel >= recipe.RequiredKitchenLevel;

        public static bool HasIngredients(RecipeDefinition recipe)
        {
            if (recipe == null) return false;
            foreach (var ingredient in recipe.Ingredients)
                if (ingredient.Goods != null && GoodsService.Count(ingredient.Goods) < ingredient.Amount) return false;
            return true;
        }

        public static CookingJob GetJob(int slot) => Data.Get(slot);

        public static int FreeSlot()
        {
            for (int slot = 0; slot < SlotCount; slot++)
                if (Data.Get(slot) == null) return slot;
            return -1;
        }

        public static bool IsReady(int slot)
        {
            var job = Data.Get(slot);
            return job != null && KitchenRules.IsReady(GameClock.UtcNow, job.ReadyTicks);
        }

        public static TimeSpan TimeLeft(int slot)
        {
            var job = Data.Get(slot);
            return job == null ? TimeSpan.Zero : FarmRules.TimeLeft(GameClock.UtcNow, job.ReadyTicks);
        }

        /// <summary>Meals waiting to be collected (for badges).</summary>
        public static int ReadyCount()
        {
            int ready = 0;
            foreach (var job in Data.Jobs)
                if (job != null && KitchenRules.IsReady(GameClock.UtcNow, job.ReadyTicks)) ready++;
            return ready;
        }

        public static KitchenResult CheckCook(RecipeDefinition recipe)
        {
            if (recipe == null || recipe.Output == null) return KitchenResult.UnknownRecipe;
            if (!IsBuilt) return KitchenResult.NotBuilt;
            if (!IsUnlocked(recipe)) return KitchenResult.RecipeLocked;
            if (FreeSlot() < 0) return KitchenResult.NoFreeSlot;
            if (!HasIngredients(recipe)) return KitchenResult.MissingIngredients;
            return KitchenResult.Success;
        }

        /// <summary>Uses up the ingredients and puts the recipe on the first free slot.</summary>
        public static KitchenResult Cook(RecipeDefinition recipe)
        {
            var check = CheckCook(recipe);
            if (check != KitchenResult.Success) return check;
            foreach (var ingredient in recipe.Ingredients)
                if (ingredient.Goods != null) GoodsService.TrySpend(ingredient.Goods.Id, ingredient.Amount);

            var now = GameClock.UtcNow;
            Data.Jobs.Add(new CookingJob
            {
                Slot = FreeSlot(), RecipeId = recipe.Id, StartTicks = now.Ticks,
                ReadyTicks = now.Ticks + TimeSpan.FromSeconds(recipe.CookSeconds).Ticks,
            });
            Changed();
            return KitchenResult.Success;
        }

        /// <summary>Moves a finished meal from its pot into the storehouse.</summary>
        public static KitchenResult Collect(int slot, out GoodsDefinition meal, out int amount)
        {
            meal = null;
            amount = 0;
            var job = Data.Get(slot);
            if (job == null) return KitchenResult.SlotEmpty;
            if (!KitchenRules.IsReady(GameClock.UtcNow, job.ReadyTicks)) return KitchenResult.NotReady;
            Data.Jobs.Remove(job);
            var recipe = GetRecipe(job.RecipeId);
            if (recipe != null && recipe.Output != null)
            {
                meal = recipe.Output;
                amount = recipe.OutputAmount;
                GoodsService.Add(meal.Id, amount);
                Progress.Report(ProgressStatIds.MealsCooked, amount);
            }
            Changed();
            return KitchenResult.Success;
        }

        /// <summary>Collects every finished meal; returns how many meals came out.</summary>
        public static int CollectAll()
        {
            var slots = new List<int>();
            foreach (var job in Data.Jobs)
                if (job != null) slots.Add(job.Slot);
            int total = 0;
            foreach (int slot in slots)
                if (Collect(slot, out _, out int amount) == KitchenResult.Success) total += amount;
            return total;
        }

        // ------------------------------------------------------------------ meals for battle

        /// <summary>Every goods that works as a battle meal, in storehouse order (a new list per call).</summary>
        public static List<GoodsDefinition> BattleMeals()
        {
            var list = new List<GoodsDefinition>();
            var catalog = GoodsService.Catalog;
            if (catalog == null) return list;
            foreach (var goods in catalog.All)
                if (goods != null && goods.IsBattleMeal) list.Add(goods);
            list.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return list;
        }

        public static IReadOnlyList<string> SelectedMeals => Data.SelectedMeals;
        public static bool IsSelected(string mealId) => Data.SelectedMeals.Contains(mealId);

        public static MealToggle ToggleMeal(GoodsDefinition meal)
        {
            if (meal == null || !meal.IsBattleMeal) return MealToggle.NotAMeal;
            var result = KitchenRules.Toggle(Data.SelectedMeals, meal.Id);
            if (result == MealToggle.Added || result == MealToggle.Removed) Changed();
            return result;
        }

        /// <summary>
        /// Called once as a battle starts: eats one of each picked meal that's in stock and returns them. Picked
        /// meals you've run out of are skipped but stay picked, so cooking more puts them back on the menu.
        /// </summary>
        public static List<GoodsDefinition> EatSelectedMeals(SaveData save)
        {
            _mealsThisRun.Clear();
            if (save == null) return new List<GoodsDefinition>();
            var catalog = GoodsService.Catalog;
            foreach (var id in save.Kitchen.SelectedMeals)
            {
                var meal = catalog != null ? catalog.Get(id) : null;
                if (meal == null || !meal.IsBattleMeal) continue;
                int have = save.Goods.Items.GetLevel(id);
                if (have <= 0) continue;
                save.Goods.Items.SetLevel(id, have - 1);
                _mealsThisRun.Add(meal);
            }
            if (_mealsThisRun.Count > 0)
            {
                SaveService.MarkDirty();
                EventBus<GoodsChangedEvent>.Raise(new GoodsChangedEvent());
            }
            return new List<GoodsDefinition>(_mealsThisRun);
        }

        public static string Describe(KitchenResult result, RecipeDefinition recipe = null) => result switch
        {
            KitchenResult.NotBuilt => "Build the Kitchen first (Castle Lv 2).",
            KitchenResult.UnknownRecipe => "Unknown recipe.",
            KitchenResult.RecipeLocked => recipe != null ? $"Needs Kitchen Lv {recipe.RequiredKitchenLevel}." : "Upgrade the Kitchen first.",
            KitchenResult.NoFreeSlot => "Every pot is busy. Collect or wait for a meal first.",
            KitchenResult.MissingIngredients => "Not enough ingredients. Grow more on the farm!",
            KitchenResult.SlotEmpty => "Nothing is cooking there.",
            KitchenResult.NotReady => "Still cooking...",
            _ => string.Empty,
        };

        private static void Changed()
        {
            SaveService.MarkDirty();
            EventBus<KitchenChangedEvent>.Raise(new KitchenChangedEvent());
        }
    }
}
