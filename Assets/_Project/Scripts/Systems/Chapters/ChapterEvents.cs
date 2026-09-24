using NinjaVillage.Core.Events;
using NinjaVillage.Gameplay.Chapters;

namespace NinjaVillage.Systems.Chapters
{
    /// <summary>A battle started in <see cref="Chapter"/> (raised by <see cref="ChapterDirector"/> in Start).</summary>
    public readonly struct ChapterStartedEvent : IGameEvent
    {
        public readonly ChapterDefinition Chapter;
        public ChapterStartedEvent(ChapterDefinition chapter) => Chapter = chapter;
    }

    /// <summary>A chapter was cleared for the first time.</summary>
    public readonly struct ChapterClearedEvent : IGameEvent
    {
        public readonly ChapterRunResult Result;
        public ChapterClearedEvent(ChapterRunResult result) => Result = result;
    }

    /// <summary>The chapter START will play changed (home screen selector).</summary>
    public readonly struct ChapterSelectedEvent : IGameEvent
    {
        public readonly ChapterDefinition Chapter;
        public ChapterSelectedEvent(ChapterDefinition chapter) => Chapter = chapter;
    }
}
