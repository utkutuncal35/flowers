using Flowers.Core;
using UnityEditor;
using UnityEngine;

namespace Flowers.Editor
{
    public static class ProgressMenu
    {
        [MenuItem("Flowers/Reset Progress")]
        public static void ResetProgress()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Flowers] Stop Play mode first: the running game would save its progress again.");
                return;
            }

            ProgressStore.Delete();
            Debug.Log($"[Flowers] Progress deleted ({ProgressStore.FilePath}). Next Play starts from level 1.");
        }

        [MenuItem("Flowers/Show Save File")]
        public static void ShowSaveFile()
        {
            EditorUtility.RevealInFinder(System.IO.File.Exists(ProgressStore.FilePath)
                ? ProgressStore.FilePath
                : Application.persistentDataPath);
        }
    }
}
