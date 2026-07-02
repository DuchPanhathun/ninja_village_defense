using System.IO;
using UnityEngine;

namespace NinjaVillage.Systems.Save
{
    /// <summary>
    /// Local JSON save/load. This is the offline fallback the Firebase Cloud Save
    /// system (EPIC 22) will sync against once configured — always write here
    /// first so the game works fully offline, then push to the cloud.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "save.json";
        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists() => File.Exists(FilePath);

        public static void Save(SaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FilePath, json);
        }

        public static SaveData Load()
        {
            if (!Exists()) return new SaveData();

            string json = File.ReadAllText(FilePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
    }
}
