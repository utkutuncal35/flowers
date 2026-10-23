using Flowers.Core;
using UnityEditor;
using UnityEngine;

namespace Flowers.Editor
{
    public static class ScreenshotMenu
    {
        [MenuItem("Flowers/Capture Screens")]
        public static void CaptureScreens()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Flowers] Stop Play mode first; the tour starts Play mode itself.");
                return;
            }

            SessionState.SetBool(ScreenshotTour.SessionKey, true);
            Debug.Log($"[Flowers] Starting screenshot tour. Keep the Game view visible. Images go to {ScreenshotTour.OutputFolder}");
            EditorApplication.EnterPlaymode();
        }
    }
}
