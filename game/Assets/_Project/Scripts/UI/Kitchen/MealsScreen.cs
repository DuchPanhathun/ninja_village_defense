using NinjaVillage.Systems.GameFlow;
using NinjaVillage.Systems.Kitchen;
using NinjaVillage.UI.Common;
using TMPro;
using UnityEngine;

namespace NinjaVillage.UI.Kitchen
{
    /// <summary>
    /// Pick the meals to eat before the next battle, from the home screen (the Kitchen in the village shows
    /// the same list next to its stoves).
    /// </summary>
    [SceneScreen(SceneNames.MainMenu)]
    public class MealsScreen : UIListScreen
    {
        public override string ScreenId => ScreenIds.Meals;
        protected override string Title => "Battle Meals";

        protected override void Populate(RectTransform content)
        {
            InfoText.text = $"Take up to {KitchenRules.MaxMeals} meals into your next battle. One of each is eaten as it starts and powers you up for that whole run. Cook more in the village Kitchen.";
            if (!KitchenService.IsBuilt && KitchenService.BattleMeals().TrueForAll(m => Systems.Farm.GoodsService.Count(m) == 0))
            {
                UIBuilder.Text(content, "Build the Kitchen in your village (Castle Lv 2), then cook your harvest into meals.",
                    UITheme.BodySize, TextAlignmentOptions.Center, UITheme.TextMuted);
                return;
            }
            MealCards.Populate(content, Refresh);
        }
    }
}
