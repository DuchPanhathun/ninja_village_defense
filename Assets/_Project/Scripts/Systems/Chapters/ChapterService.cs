using System.Collections.Generic;
using NinjaVillage.Core.Data;
using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Chapters;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Chapters
{
    /// <summary>
    /// Chapter progression for the UI and the battle: the ordered chapter list, which are unlocked or
    /// cleared, the one START plays, and recording a finished run (best wave, first-clear rewards, unlocking
    /// the next chapter and moving the selection to it). State lives in <c>SaveService.Data.Chapters</c>;
    /// rules in <see cref="ChapterRules"/>.
    /// </summary>
    public static class ChapterService
    {
        public static ChapterCatalog Catalog => CatalogLoader.Load<ChapterCatalog>();

        private static ChapterSaveData Section => SaveService.Data.Chapters;

        /// <summary>The last run's chapter result (for the game-over panel); null before any chapter run ended.</summary>
        public static ChapterRunResult LastResult { get; private set; }

        /// <summary>Chapters ordered by number. A new list per call (UI use).</summary>
        public static List<ChapterDefinition> GetChapters()
        {
            var list = new List<ChapterDefinition>();
            var catalog = Catalog;
            if (catalog == null) return list;
            foreach (var chapter in catalog.All)
                if (chapter != null) list.Add(chapter);
            list.Sort((a, b) => a.Number.CompareTo(b.Number));
            return list;
        }

        public static ChapterDefinition Get(string id)
        {
            var catalog = Catalog;
            return catalog != null ? catalog.Get(id) : null;
        }

        public static ChapterDefinition GetByNumber(int number)
        {
            foreach (var chapter in GetChapters())
                if (chapter.Number == number) return chapter;
            return null;
        }

        public static int HighestCleared => Section.HighestCleared;

        public static bool IsUnlocked(ChapterDefinition chapter) =>
            chapter != null && ChapterRules.IsUnlocked(chapter.Number, Section.HighestCleared);

        public static bool IsCleared(ChapterDefinition chapter) =>
            chapter != null && ChapterRules.IsCleared(chapter.Number, Section.HighestCleared);

        public static int BestWave(ChapterDefinition chapter) =>
            chapter != null ? Section.BestWaves.GetLevel(chapter.Id) : 0;

        /// <summary>
        /// The chapter START plays: the saved selection if it's still unlocked, else the furthest unlocked
        /// chapter (so a new player starts at chapter 1 and a veteran at their frontier).
        /// </summary>
        public static ChapterDefinition Selected
        {
            get
            {
                var selected = Get(Section.SelectedId);
                if (selected != null && IsUnlocked(selected)) return selected;
                ChapterDefinition frontier = null;
                foreach (var chapter in GetChapters())
                    if (IsUnlocked(chapter)) frontier = chapter;
                return frontier;
            }
        }

        public static bool TrySelect(ChapterDefinition chapter)
        {
            if (!IsUnlocked(chapter)) return false;
            if (Section.SelectedId == chapter.Id) return true;
            Section.SelectedId = chapter.Id;
            SaveService.MarkDirty();
            EventBus<ChapterSelectedEvent>.Raise(new ChapterSelectedEvent(chapter));
            return true;
        }

        /// <summary>Records a finished run in <paramref name="chapter"/>, pays a first clear and advances the selection.</summary>
        public static ChapterRunResult RecordRun(ChapterDefinition chapter, bool victory, int waveReached)
        {
            if (chapter == null) return LastResult = null;

            var result = ChapterRules.Record(Section, chapter.Id, chapter.Number, chapter.WaveCount, victory, waveReached,
                chapter.ClearCoins, chapter.ClearGems);
            if (result.FirstClear)
            {
                CurrencyService.Grant(CurrencyType.Coins, result.RewardCoins, $"chapter_{chapter.Number}_clear");
                CurrencyService.Grant(CurrencyType.Gems, result.RewardGems, $"chapter_{chapter.Number}_clear");
                var next = GetByNumber(chapter.Number + 1);
                if (next != null)
                {
                    result.UnlockedChapterId = next.Id;
                    Section.SelectedId = next.Id; // START now plays the new chapter
                }
                EventBus<ChapterClearedEvent>.Raise(new ChapterClearedEvent(result));
            }
            SaveService.MarkDirty();
            return LastResult = result;
        }
    }
}
