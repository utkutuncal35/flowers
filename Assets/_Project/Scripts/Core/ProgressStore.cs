using System;
using System.IO;
using Flowers.Levels;
using UnityEngine;

namespace Flowers.Core
{
    /// <summary>
    /// Saves <see cref="ProgressData"/> as JSON in Application.persistentDataPath (app storage on the phone).
    /// Writes go to a temp file first and are then moved into place, so a crash mid-save can't
    /// leave a half-written file. A broken file is set aside and the game starts fresh instead of failing.
    /// </summary>
    public static class ProgressStore
    {
        private const string FileName = "progress.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
        private static string TempPath => FilePath + ".tmp";

        public static ProgressData Load()
        {
            // If the app died between "delete old" and "move new" in Save, only the temp file exists.
            if (!File.Exists(FilePath) && File.Exists(TempPath))
                File.Move(TempPath, FilePath);

            if (!File.Exists(FilePath))
                return new ProgressData();

            try
            {
                var data = JsonUtility.FromJson<ProgressData>(File.ReadAllText(FilePath));
                return data ?? new ProgressData();
            }
            catch (Exception e)
            {
                string backup = FilePath + ".broken";
                Debug.LogWarning($"[Flowers] Save file was unreadable, starting fresh. Kept a copy at {backup}. ({e.Message})");
                try { File.Copy(FilePath, backup, true); } catch (Exception) { /* best effort */ }
                return new ProgressData();
            }
        }

        public static void Save(ProgressData data)
        {
            try
            {
                File.WriteAllText(TempPath, JsonUtility.ToJson(data, true));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(TempPath, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Flowers] Could not save progress to {FilePath}: {e.Message}");
            }
        }

        public static void Delete()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            if (File.Exists(TempPath)) File.Delete(TempPath);
        }
    }
}
