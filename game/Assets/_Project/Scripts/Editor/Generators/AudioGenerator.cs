using System.Collections.Generic;
using NinjaVillage.Systems.Audio;

namespace NinjaVillage.EditorTools.Generators
{
    /// <summary>
    /// Creates the AudioLibrary with one entry per <c>AudioCueIds</c> constant (EPIC 18). Entries start
    /// without clips — the AudioManager synthesizes a placeholder for those — so adding real audio is
    /// just dragging clips onto the matching entry. Existing entries (and their clips) are kept.
    /// </summary>
    public static class AudioGenerator
    {
        [ContentGenerator("Audio library", 30)]
        public static void Generate()
        {
            var library = ContentGen.CreateOrLoad<AudioLibrary>($"{ContentGen.CatalogRoot}/AudioLibrary.asset");

            var entries = new List<AudioCueEntry>(library.Cues);
            var existing = new HashSet<string>();
            foreach (var cue in entries)
                if (cue != null && !string.IsNullOrEmpty(cue.Id)) existing.Add(cue.Id);

            foreach (var id in AudioCueDefaults.AllCueIds)
                if (!existing.Contains(id)) entries.Add(new AudioCueEntry(id, AudioCueDefaults.For(id)));

            library.EditorSetCues(entries);
        }
    }
}
