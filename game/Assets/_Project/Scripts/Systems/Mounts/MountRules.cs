using UnityEngine;

namespace NinjaVillage.Systems.Mounts
{
    /// <summary>Mount levels as pure functions: bonuses grow 10% per level up to Lv 10; each level costs a bit more.</summary>
    public static class MountRules
    {
        public const int MaxLevel = 10;

        /// <summary>Multiplier on a mount's Lv 1 bonuses.</summary>
        public static float LevelScale(int level) => 1f + 0.1f * (Mathf.Clamp(level, 1, MaxLevel) - 1);

        /// <summary>Coins to go from <paramref name="level"/> to the next.</summary>
        public static int UpgradeCost(int level) => 250 * Mathf.Max(1, level);
    }
}
