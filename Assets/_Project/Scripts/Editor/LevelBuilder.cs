using System.Collections.Generic;
using Flowers.Levels;
using UnityEditor;
using UnityEngine;

namespace Flowers.Editor
{
    /// <summary>
    /// Writes the levels from <see cref="LevelCatalog"/> (design) and <see cref="LevelTuning"/> (moves and stars from
    /// the tuner) into LevelData assets, and puts them in the LevelList in catalog order.
    /// </summary>
    public static class LevelBuilder
    {
        public const string LevelsFolder = "Assets/_Project/Data/Levels";
        public const string LevelListPath = "Assets/_Project/Data/LevelList.asset";

        // The three sample levels from before the 30-level catalog.
        private static readonly string[] LegacyLevels = { "Level01_Tutorial", "Level02_TwoFlowers", "Level03_FewMoves" };

        [MenuItem("Flowers/Rebuild Levels")]
        public static void RebuildLevelsMenu()
        {
            bool ok = EditorUtility.DisplayDialog("Rebuild Levels",
                $"This rewrites all {LevelCatalog.All.Length} level assets from LevelCatalog and the tuned moves/stars.\n\n" +
                "Changes made to those levels in the Inspector will be lost.", "Rebuild", "Cancel");
            if (!ok) return;

            Build(overwriteExisting: true);
            Debug.Log($"[Flowers] Rebuilt {LevelCatalog.All.Length} levels and the LevelList.");
        }

        /// <summary>
        /// overwriteExisting = false (Setup Scene): only creates missing level assets, and fills the LevelList
        /// only if it is missing or doesn't contain every catalog level yet.
        /// </summary>
        public static void Build(bool overwriteExisting)
        {
            EditorFolders.Ensure(LevelsFolder);
            foreach (var legacy in LegacyLevels)
            {
                string path = $"{LevelsFolder}/{legacy}.asset";
                if (AssetDatabase.LoadAssetAtPath<LevelData>(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                    Debug.Log($"[Flowers] Removed old sample level {legacy}.");
                }
            }

            var levels = new List<LevelData>();
            bool createdAny = false;
            foreach (var spec in LevelCatalog.All)
            {
                string path = $"{LevelsFolder}/{spec.Name}.asset";
                var level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                bool isNew = level == null;
                if (isNew)
                {
                    level = ScriptableObject.CreateInstance<LevelData>();
                    AssetDatabase.CreateAsset(level, path);
                    createdAny = true;
                }

                if (isNew || overwriteExisting)
                {
                    if (!LevelTuning.Values.TryGetValue(spec.Name, out var tuning))
                    {
                        Debug.LogError($"[Flowers] {spec.Name} has no tuned moves in LevelTuning. Run the level tuner (Tools/LevelTuner). Using 25 moves.");
                        tuning = (25, 5, 10);
                    }
                    level.EditorSetup(string.Join("\n", spec.Layout), spec.Types, tuning.moves, spec.Goals,
                        tuning.twoStar, tuning.threeStar);
                    EditorUtility.SetDirty(level);
                }
                levels.Add(level);
            }

            var list = AssetDatabase.LoadAssetAtPath<LevelList>(LevelListPath);
            if (list == null)
            {
                list = ScriptableObject.CreateInstance<LevelList>();
                AssetDatabase.CreateAsset(list, LevelListPath);
            }
            if (overwriteExisting || createdAny || !ContainsAll(list, levels))
            {
                list.EditorSetLevels(levels.ToArray());
                EditorUtility.SetDirty(list);
            }
            AssetDatabase.SaveAssets();
        }

        private static bool ContainsAll(LevelList list, List<LevelData> levels)
        {
            var inList = new HashSet<LevelData>();
            for (int i = 0; i < list.Count; i++) inList.Add(list.Get(i));
            foreach (var level in levels)
                if (!inList.Contains(level)) return false;
            return true;
        }
    }
}
