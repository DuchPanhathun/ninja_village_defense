using System;
using System.Collections.Generic;

namespace NinjaVillage.Core.Events
{
    /// <summary>
    /// A lightweight, type-safe, global publish/subscribe hub.
    ///
    /// Systems communicate by raising and listening for <see cref="IGameEvent"/> structs
    /// instead of holding direct references to one another. This keeps the Player, Enemy,
    /// Weapon, Skill, Wave, Loot and UI systems decoupled — exactly the modular architecture
    /// the design doc calls for.
    ///
    /// Usage:
    /// <code>
    /// EventBus&lt;PlayerDamagedEvent&gt;.Subscribe(OnPlayerDamaged);   // in OnEnable
    /// EventBus&lt;PlayerDamagedEvent&gt;.Unsubscribe(OnPlayerDamaged); // in OnDisable
    /// EventBus&lt;PlayerDamagedEvent&gt;.Raise(new PlayerDamagedEvent(amount));
    /// </code>
    /// </summary>
    /// <typeparam name="T">A struct implementing <see cref="IGameEvent"/>.</typeparam>
    public static class EventBus<T> where T : struct, IGameEvent
    {
        private static event Action<T> Handlers;

        static EventBus()
        {
            // Register this channel so EventBusRegistry.ClearAll() can reset it.
            EventBusRegistry.Register(Clear);
        }

        /// <summary>Registers a listener. Safe to call in OnEnable.</summary>
        public static void Subscribe(Action<T> handler) => Handlers += handler;

        /// <summary>Removes a listener. Always mirror a Subscribe with this in OnDisable.</summary>
        public static void Unsubscribe(Action<T> handler) => Handlers -= handler;

        /// <summary>Publishes an event to every current listener.</summary>
        public static void Raise(T evt)
        {
            // Snapshot via the delegate's null check so a handler that unsubscribes
            // mid-dispatch does not throw.
            Handlers?.Invoke(evt);
        }

        /// <summary>
        /// Clears every listener for this event type. Call between scene loads or from
        /// tests to avoid dangling references to destroyed objects.
        /// </summary>
        public static void Clear() => Handlers = null;
    }

    /// <summary>
    /// Non-generic helper for bulk operations across all event channels (e.g. on scene reset).
    /// Channels register their Clear action the first time they are used.
    /// </summary>
    public static class EventBusRegistry
    {
        private static readonly List<Action> ClearActions = new();

        internal static void Register(Action clearAction)
        {
            if (!ClearActions.Contains(clearAction))
                ClearActions.Add(clearAction);
        }

        /// <summary>Clears every known event channel. Call when tearing down a battle scene.</summary>
        public static void ClearAll()
        {
            foreach (var clear in ClearActions)
                clear?.Invoke();
        }
    }
}
