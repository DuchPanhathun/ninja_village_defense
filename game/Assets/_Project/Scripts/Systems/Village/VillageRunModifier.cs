using NinjaVillage.Systems.Meta;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    /// <summary>
    /// Where the village meets the battle: at run start, applies the Dojo's attack bonus
    /// (goal.text: Lv1 +2% … Lv20 +80%) and every Shrine blessing rank (+Health, +Critical, +Luck,
    /// +Coins) to the player. Registered automatically; runs at <see cref="RunModifierOrder.Village"/>.
    /// </summary>
    public sealed class VillageRunModifier : IRunStartModifier
    {
        public int Order => RunModifierOrder.Village;

        public void Apply(RunStartContext context)
        {
            var save = context.Save ?? SaveService.Data;
            VillageService.ComputeBonuses(save).ApplyTo(context);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => RunStartModifiers.Register(new VillageRunModifier());
    }
}
