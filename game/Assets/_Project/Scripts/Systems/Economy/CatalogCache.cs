using NinjaVillage.Core.Data;
using UnityEngine;

namespace NinjaVillage.Systems.Economy
{
    /// <summary>
    /// Wraps <see cref="CatalogLoader.Load{T}"/> for code that asks for a catalog often (UI refreshes,
    /// map labels). CatalogLoader already caches found assets, but when an asset is missing (content
    /// not generated yet) it would log a warning on every call — this retries at most every few
    /// seconds so a missing catalog degrades to "empty" instead of flooding the console.
    /// </summary>
    public static class CatalogCache<T> where T : ScriptableObject
    {
        private const float RetrySeconds = 10f;

        private static T _value;
        private static float _nextRetryTime = float.MinValue;

        public static T Get()
        {
            if (_value != null) return _value;

            float now = Time.realtimeSinceStartup;
            if (now < _nextRetryTime) return null;

            _value = CatalogLoader.Load<T>();
            if (_value == null) _nextRetryTime = now + RetrySeconds;
            return _value;
        }
    }
}
