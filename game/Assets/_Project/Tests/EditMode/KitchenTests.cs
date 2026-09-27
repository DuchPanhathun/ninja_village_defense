using System;
using System.Collections.Generic;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Kitchen;
using NinjaVillage.Systems.Meta;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;
using NUnit.Framework;
using UnityEngine;

namespace NinjaVillage.Tests
{
    public class KitchenTests
    {
        private static readonly DateTime T0 = new(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Stoves_OpenWithTheKitchen()
        {
            Assert.AreEqual(0, KitchenRules.Slots(0, 3f), "no kitchen, no stove");
            Assert.AreEqual(1, KitchenRules.Slots(1, 0f), "a built kitchen always cooks");
            Assert.AreEqual(2, KitchenRules.Slots(3, 1.9999999f));
            Assert.AreEqual(3, KitchenRules.Slots(5, 3f));
        }

        [Test]
        public void Cooking_IsDoneOnTime()
        {
            long start = T0.Ticks, ready = T0.AddMinutes(10).Ticks;
            Assert.IsFalse(KitchenRules.IsReady(T0.AddMinutes(9), ready));
            Assert.IsTrue(KitchenRules.IsReady(T0.AddMinutes(10), ready));
            Assert.AreEqual(0.5f, KitchenRules.Progress(T0.AddMinutes(5), start, ready), 0.001f);
        }

        [Test]
        public void AtMostTwoMeals_CanBePacked()
        {
            var picked = new List<string>();
            Assert.AreEqual(MealToggle.Added, KitchenRules.Toggle(picked, "onigiri"));
            Assert.AreEqual(MealToggle.Added, KitchenRules.Toggle(picked, "yakitori"));
            Assert.AreEqual(MealToggle.Full, KitchenRules.Toggle(picked, "noodle"));
            Assert.AreEqual(MealToggle.Removed, KitchenRules.Toggle(picked, "onigiri"));
            Assert.AreEqual(MealToggle.Added, KitchenRules.Toggle(picked, "noodle"));
            CollectionAssert.AreEqual(new[] { "yakitori", "noodle" }, picked);
        }

        [Test]
        public void StartingABattle_EatsOneOfEachPackedMeal_AndAppliesItsBoost()
        {
            var onigiri = GoodsService.Get("onigiri");
            Assume.That(onigiri != null && onigiri.IsBattleMeal, "goods catalog missing — run the content generator");

            var save = new SaveData();
            save.Migrate();
            save.Goods.Items.SetLevel("onigiri", 2);
            save.Kitchen.SelectedMeals.AddRange(new[] { "onigiri", "yakitori" }); // no yakitori in stock

            var context = new RunStartContext { Save = save, BaseMaxHealth = 100f };
            new MealRunModifier().Apply(context);

            Assert.AreEqual(1, save.Goods.Items.GetLevel("onigiri"), "exactly one onigiri eaten");
            Assert.AreEqual(1f + onigiri.MealValue, context.MaxHealthMultiplier, 0.0001f);
            Assert.AreEqual(1, KitchenService.MealsThisRun.Count, "the missing yakitori is skipped");
            CollectionAssert.AreEqual(new[] { "onigiri", "yakitori" }, save.Kitchen.SelectedMeals, "packed meals stay packed");

            save.Goods.Items.SetLevel("onigiri", 0);
            var next = new RunStartContext { Save = save, BaseMaxHealth = 100f };
            new MealRunModifier().Apply(next);
            Assert.AreEqual(1f, next.MaxHealthMultiplier, 0.0001f, "nothing to eat, no boost");
        }

        [Test]
        public void Recipes_UseCropsThatAreOpenByThen_AndPayMoreThanTheirIngredients()
        {
            var recipes = KitchenService.GetRecipes();
            Assume.That(recipes.Count > 0, "recipe catalog missing — run the content generator");
            var kitchen = VillageService.Get(BuildingIds.Kitchen);
            Assert.IsNotNull(kitchen, "Kitchen building missing");

            foreach (var recipe in recipes)
            {
                Assert.IsNotNull(recipe.Output, recipe.Id);
                Assert.IsTrue(recipe.Output.IsBattleMeal, $"{recipe.Id} cooks a battle meal");
                int castle = VillageRules.CastleLevelNeededFor(recipe.RequiredKitchenLevel, kitchen.LevelsPerCastleLevel, kitchen.RequiredCastleLevel);
                int ingredientValue = 0;
                foreach (var ingredient in recipe.Ingredients)
                {
                    Assert.IsNotNull(ingredient.Goods, recipe.Id);
                    ingredientValue += ingredient.Goods.SellPrice * ingredient.Amount;
                    var crop = FarmService.GetCrop(ingredient.Goods.Id);
                    if (crop != null)
                        Assert.LessOrEqual(crop.RequiredCastleLevel, castle, $"{recipe.Id} needs {crop.Id} before it can be grown");
                }
                Assert.Greater(recipe.Output.SellPrice * recipe.OutputAmount, ingredientValue, $"{recipe.Id} is worth cooking");
            }
        }
    }
}
