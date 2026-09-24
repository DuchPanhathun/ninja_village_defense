using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Chapters
{
    /// <summary>What happened to chapter progress when a run ended (shown on the game-over panel).</summary>
    public sealed class ChapterRunResult
    {
        public string ChapterId;
        public int ChapterNumber;
        public bool Victory;
        public int WaveReached;
        public int TotalWaves;
        public bool NewBestWave;
        /// <summary>True only the first time the chapter is cleared — the one time rewards are paid.</summary>
        public bool FirstClear;
        public int RewardCoins;
        public int RewardGems;
        /// <summary>Id of the chapter this clear unlocked (null when none, or it was already unlocked).</summary>
        public string UnlockedChapterId;
    }

    /// <summary>
    /// Pure chapter progression rules over <see cref="ChapterSaveData"/> (no catalog, no Unity objects),
    /// so they're unit-testable: chapters unlock in order, and each pays its reward on the first clear only.
    /// </summary>
    public static class ChapterRules
    {
        public static bool IsUnlocked(int number, int highestCleared) => number >= 1 && number <= highestCleared + 1;

        public static bool IsCleared(int number, int highestCleared) => number >= 1 && number <= highestCleared;

        /// <summary>
        /// Folds a finished run into <paramref name="data"/>: best wave, and on a victory the clear itself.
        /// Rewards are reported, not paid (the service pays them).
        /// </summary>
        public static ChapterRunResult Record(ChapterSaveData data, string chapterId, int number, int totalWaves,
            bool victory, int waveReached, int clearCoins, int clearGems)
        {
            var result = new ChapterRunResult
            {
                ChapterId = chapterId,
                ChapterNumber = number,
                Victory = victory,
                WaveReached = waveReached,
                TotalWaves = totalWaves,
            };
            if (data == null || string.IsNullOrEmpty(chapterId)) return result;
            data.BestWaves ??= new System.Collections.Generic.List<IdLevelEntry>();

            int best = data.BestWaves.GetLevel(chapterId);
            int reached = victory ? Mathf.Max(waveReached, totalWaves) : waveReached;
            if (reached > best)
            {
                data.BestWaves.SetLevel(chapterId, reached);
                result.NewBestWave = true;
            }

            if (victory && !IsCleared(number, data.HighestCleared) && IsUnlocked(number, data.HighestCleared))
            {
                result.FirstClear = true;
                result.RewardCoins = Mathf.Max(0, clearCoins);
                result.RewardGems = Mathf.Max(0, clearGems);
                data.HighestCleared = number;
            }
            return result;
        }
    }
}
