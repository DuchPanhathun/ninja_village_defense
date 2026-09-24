using System;
using System.Collections.Generic;
using NinjaVillage.Core.ScriptableObjects;
using NinjaVillage.Systems.Farm;
using UnityEngine;

namespace NinjaVillage.Systems.Kitchen
{
    /// <summary>Goods a recipe uses up, and how many.</summary>
    [Serializable]
    public struct Ingredient
    {
        public GoodsDefinition Goods;
        [Min(1)] public int Amount;
    }

    /// <summary>
    /// A Kitchen recipe (EPIC 24 Phase 2): storehouse ingredients in, one meal out after a real-time cook.
    /// The meal's battle boost lives on its <see cref="GoodsDefinition"/>. Higher Kitchen levels unlock more recipes.
    /// </summary>
    [CreateAssetMenu(fileName = "Recipe", menuName = "Ninja Village/Village/Recipe")]
    public class RecipeDefinition : DescriptiveScriptableObject
    {
        [SerializeField] private GoodsDefinition output;
        [SerializeField, Min(1)] private int outputAmount = 1;
        [SerializeField] private List<Ingredient> ingredients = new();
        [Tooltip("Real-time seconds from starting to cook until the meal can be collected.")]
        [SerializeField, Min(1f)] private float cookSeconds = 120f;
        [SerializeField, Min(1)] private int requiredKitchenLevel = 1;

        public GoodsDefinition Output => output;
        public int OutputAmount => outputAmount;
        public IReadOnlyList<Ingredient> Ingredients => ingredients;
        public float CookSeconds => cookSeconds;
        public int RequiredKitchenLevel => requiredKitchenLevel;
        public string NameOrId => output != null ? output.NameOrId : string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;
    }
}
