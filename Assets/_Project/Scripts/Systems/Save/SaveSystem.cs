using System;
using System.IO;
using UnityEngine;

namespace NinjaVillage.Systems.Save
{
    /// <summary>
    /// Local JSON save/load. This is the offline source of truth the Firebase Cloud
    /// Save sync (EPIC 22) reconciles against — always write here first so the game
    /// works fully offline, then push to the cloud.
    ///
    /// Writes are atomic (temp file + replace) and keep one backup, so a crash or
    /// power loss mid-write can't corrupt the only copy of the player's progress.
    /// Prefer <see cref="SaveService"/> in gameplay code; this is the raw file layer.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "save.json";

        /// <summary>Tests point saves at a temp folder so they never touch the real save. Null = persistentDataPath.</summary>
        public static string OverrideDirectory;

        private static string FilePath => Path.Combine(OverrideDirectory ?? Application.persistentDataPath, FileName);
        private static string TempPath => FilePath + ".tmp";
        private static string BackupPath => FilePath + ".bak";

        public static bool Exists() => File.Exists(FilePath) || File.Exists(BackupPath);

        public static void Save(SaveData data)
        {
            if (data == null) return;

            data.LastSavedUtcTicks = DateTime.UtcNow.Ticks;
            string json = ToJson(data);

            try
            {
                File.WriteAllText(TempPath, json);
                if (File.Exists(FilePath))
                    File.Replace(TempPath, FilePath, BackupPath);
                else
                    File.Move(TempPath, FilePath);
            }
            catch (Exception e)
            {
                // Some Android filesystems don't support File.Replace — fall back to a plain overwrite.
                Debug.LogWarning($"SaveSystem: atomic replace failed ({e.Message}); writing directly.");
                File.WriteAllText(FilePath, json);
            }
        }

        public static SaveData Load()
        {
            var data = TryRead(FilePath) ?? TryRead(BackupPath) ?? new SaveData();
            data.Migrate();
            return data;
        }

        /// <summary>Deletes the save and its backup (settings "reset progress", tests).</summary>
        public static void Delete()
        {
            foreach (var path in new[] { FilePath, TempPath, BackupPath })
                if (File.Exists(path)) File.Delete(path);
        }

        public static string ToJson(SaveData data) => JsonUtility.ToJson(data, true);

        /// <summary>Parses save JSON (e.g. a cloud copy). Returns null when the JSON is unusable.</summary>
        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(json);
                data?.Migrate();
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveSystem: could not parse save JSON ({e.Message}).");
                return null;
            }
        }

        private static SaveData TryRead(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveSystem: '{Path.GetFileName(path)}' is unreadable ({e.Message}).");
                return null;
            }
        }
    }
}
