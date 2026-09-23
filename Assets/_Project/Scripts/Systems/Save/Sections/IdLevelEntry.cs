using System;
using System.Collections.Generic;

namespace NinjaVillage.Systems.Save
{
    /// <summary>
    /// JsonUtility can't serialize dictionaries, so "id → level" style state
    /// (heroes, pets, buildings, weapons) is stored as a list of these.
    /// </summary>
    [Serializable]
    public class IdLevelEntry
    {
        public string Id;
        public int Level;

        public IdLevelEntry() { }

        public IdLevelEntry(string id, int level)
        {
            Id = id;
            Level = level;
        }
    }

    /// <summary>Lookup helpers over <see cref="IdLevelEntry"/> lists.</summary>
    public static class IdLevelListExtensions
    {
        public static IdLevelEntry FindById(this List<IdLevelEntry> list, string id)
        {
            if (list == null || string.IsNullOrEmpty(id)) return null;
            foreach (var entry in list)
                if (entry != null && entry.Id == id) return entry;
            return null;
        }

        /// <summary>0 when the id isn't present (i.e. not owned / not built).</summary>
        public static int GetLevel(this List<IdLevelEntry> list, string id) => list.FindById(id)?.Level ?? 0;

        public static bool ContainsId(this List<IdLevelEntry> list, string id) => list.FindById(id) != null;

        /// <summary>Sets the level, adding the entry if missing.</summary>
        public static IdLevelEntry SetLevel(this List<IdLevelEntry> list, string id, int level)
        {
            var entry = list.FindById(id);
            if (entry == null)
            {
                entry = new IdLevelEntry(id, level);
                list.Add(entry);
            }
            else
            {
                entry.Level = level;
            }
            return entry;
        }
    }
}
