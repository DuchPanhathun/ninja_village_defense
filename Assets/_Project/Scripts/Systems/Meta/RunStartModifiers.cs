using System.Collections.Generic;
using UnityEngine;

namespace NinjaVillage.Systems.Meta
{
    /// <summary>
    /// A meta-progression source that shapes a battle run as it starts — the selected
    /// hero, Dojo/Shrine bonuses, talents, Forge weapon levels, equipped gear, pets...
    /// Implementations are plain C# objects registered in <see cref="RunStartModifiers"/>.
    /// </summary>
    public interface IRunStartModifier
    {
        /// <summary>Lower runs first. Use the <see cref="RunModifierOrder"/> constants.</summary>
        int Order { get; }
        void Apply(RunStartContext context);
    }

    /// <summary>Conventional ordering so e.g. the hero's base weapon is chosen before the Forge levels it.</summary>
    public static class RunModifierOrder
    {
        public const int Hero = 100;
        public const int Inventory = 200;
        public const int Village = 300;
        public const int Talents = 400;
        public const int Meals = 450;
        public const int Mounts = 480;
        public const int Pets = 500;
        public const int LiveOps = 600;
        public const int Store = 700;
    }

    /// <summary>
    /// Registry of <see cref="IRunStartModifier"/>s. Each system registers itself once,
    /// typically from a <c>[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]</c>
    /// method, so no scene or shared file has to know about every system.
    /// Registering the same type twice replaces the earlier instance.
    /// </summary>
    public static class RunStartModifiers
    {
        private static readonly List<IRunStartModifier> Modifiers = new();

        public static IReadOnlyList<IRunStartModifier> All => Modifiers;

        public static void Register(IRunStartModifier modifier)
        {
            if (modifier == null) return;
            Modifiers.RemoveAll(m => m.GetType() == modifier.GetType());
            Modifiers.Add(modifier);
        }

        public static void Unregister(IRunStartModifier modifier) => Modifiers.Remove(modifier);

        public static void ApplyAll(RunStartContext context)
        {
            var ordered = new List<IRunStartModifier>(Modifiers);
            ordered.Sort((a, b) => a.Order.CompareTo(b.Order));

            foreach (var modifier in ordered)
            {
                try
                {
                    modifier.Apply(context);
                }
                catch (System.Exception e)
                {
                    // One broken system must not stop the run from starting.
                    Debug.LogException(e);
                }
            }
        }
    }
}
