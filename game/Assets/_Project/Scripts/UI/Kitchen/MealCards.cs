using NinjaVillage.Core.Audio;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Kitchen;
using NinjaVillage.UI.Common;
using UnityEngine;

namespace NinjaVillage.UI.Kitchen
{
    /// <summary>
    /// The "Meals for the next battle" list shared by the Kitchen and the home screen's Meals picker: every
    /// battle meal with its boost and how many are stored, and Take along / Put back (two at most).
    /// </summary>
    public static class MealCards
    {
        public static void Populate(RectTransform content, System.Action refresh)
        {
            UIBuilder.SectionHeader(content, $"Meals for the next battle  {KitchenService.SelectedMeals.Count}/{KitchenRules.MaxMeals}");
            foreach (var meal in KitchenService.BattleMeals())
            {
                int count = GoodsService.Count(meal);
                bool picked = KitchenService.IsSelected(meal.Id);
                string body = $"<color=#9CFF8A>{meal.MealEffect}</color> for one battle\nStored: {count}";
                if (picked && count == 0) body += "  <color=#FF8A7A>none left, so it's skipped</color>";
                var actions = UIBuilder.ActionCard(content, picked ? $"{meal.NameOrId}  ·  TAKING" : meal.NameOrId, body,
                    out _, out _, picked ? UITheme.Gold : UITheme.Text, meal.Icon);
                var m = meal;
                var button = UIBuilder.SmallButton(actions.transform, picked ? "Put back" : "Take along", () => Toggle(m, refresh),
                    picked ? UITheme.ButtonSecondary : UITheme.Button, 280f);
                if (!picked && count == 0) UIBuilder.SetEnabled(button, false);
            }
        }

        private static void Toggle(GoodsDefinition meal, System.Action refresh)
        {
            switch (KitchenService.ToggleMeal(meal))
            {
                case MealToggle.Added:
                    Sfx.Play(AudioCueIds.UiUpgrade);
                    UIScreenNavigator.Instance.Toast($"{meal.NameOrId} packed: {meal.MealEffect} next battle");
                    break;
                case MealToggle.Removed:
                    Sfx.Play(AudioCueIds.UiBack);
                    break;
                case MealToggle.Full:
                    Sfx.Play(AudioCueIds.UiError);
                    UIScreenNavigator.Instance.Toast($"You can take {KitchenRules.MaxMeals} meals. Put one back first.");
                    break;
            }
            refresh?.Invoke();
        }
    }
}
