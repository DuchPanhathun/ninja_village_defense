using System;
using System.Collections.Generic;
using System.Globalization;

namespace NinjaVillage.Core.Config
{
    /// <summary>
    /// Server-tunable key/value settings (balance numbers, event toggles, feature flags).
    /// The backend fills this from Firebase Remote Config on device; everywhere else (Editor,
    /// offline, before the fetch lands) callers get the default they pass in. Gameplay code
    /// reads through here so it never depends on Firebase directly.
    /// </summary>
    public static class RemoteValues
    {
        private static readonly Dictionary<string, string> Values = new();

        /// <summary>Raised after a batch of values is applied (e.g. a Remote Config fetch).</summary>
        public static event Action Changed;

        public static bool Has(string key) => Values.ContainsKey(key);

        public static string GetString(string key, string fallback = "") =>
            Values.TryGetValue(key, out var v) ? v : fallback;

        public static bool GetBool(string key, bool fallback = false) =>
            Values.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : fallback;

        public static int GetInt(string key, int fallback = 0) =>
            Values.TryGetValue(key, out var v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : fallback;

        public static float GetFloat(string key, float fallback = 0f) =>
            Values.TryGetValue(key, out var v) && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : fallback;

        /// <summary>Replaces/adds values (invariant-culture strings) and notifies listeners once.</summary>
        public static void Apply(IEnumerable<KeyValuePair<string, string>> values)
        {
            foreach (var pair in values)
                Values[pair.Key] = pair.Value;
            Changed?.Invoke();
        }

        public static void Set(string key, string value)
        {
            Values[key] = value;
            Changed?.Invoke();
        }

        /// <summary>Tests only.</summary>
        public static void Clear() => Values.Clear();

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Values.Clear();
            Changed = null;
        }
    }
}
