using NinjaVillage.Systems.Meta;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Talents;
using UnityEngine;

namespace NinjaVillage.Systems.Kitchen
{
    /// <summary>
    /// Eats the meals picked in the Kitchen as a battle starts (<see cref="RunModifierOrder.Meals"/>): one of
    /// each is used up and its boost applies for this run only, the same way a talent's stat would.
    /// </summary>
    public sealed class MealRunModifier : IRunStartModifier
    {
        public int Order => RunModifierOrder.Meals;

        public void Apply(RunStartContext context)
        {
            foreach (var meal in KitchenService.EatSelectedMeals(context.Save ?? SaveService.Data))
                TalentRunModifier.ApplyStat(context, meal.MealStat, meal.MealValue);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => RunStartModifiers.Register(new MealRunModifier());
    }
}
