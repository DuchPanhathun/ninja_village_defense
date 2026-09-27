using NinjaVillage.Core.Events;
using NinjaVillage.Core.ScriptableObjects;

namespace NinjaVillage.Systems.Evolution
{
    /// <summary>Raised when the player first unlocks an evolution by satisfying its recipe.</summary>
    public readonly struct EvolutionUnlockedEvent : IGameEvent
    {
        public readonly EvolutionRecipe Recipe;
        public EvolutionUnlockedEvent(EvolutionRecipe recipe) => Recipe = recipe;
    }
}
