#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Flowers.Core
{
    /// <summary>
    /// Editor-only: started by Flowers → Capture Screens. Visits every screen (title, map, level popup, game,
    /// win, lose), saves a screenshot of each to the project's Screenshots/ folder, then leaves Play mode.
    /// Lets the screens be reviewed as images without someone describing them.
    /// </summary>
    public class ScreenshotTour : MonoBehaviour
    {
        public const string SessionKey = "Flowers.ScreenshotTour";

        public static string OutputFolder => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            if (!SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, false);
            new GameObject("Screenshot Tour").AddComponent<ScreenshotTour>();
        }

        private IEnumerator Start()
        {
            var controller = FindFirstObjectByType<LevelController>();
            if (controller == null)
            {
                Debug.LogError("[Flowers] Screenshot tour: no LevelController in the scene. Run Flowers → Setup Scene.");
                EditorApplication.isPlaying = false;
                yield break;
            }
            Directory.CreateDirectory(OutputFolder);

            yield return new WaitForSecondsRealtime(1.2f);
            yield return Capture("1_title");

            controller.ShowMap();
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture("2_map");

            controller.ShowLevelStart(0);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Capture("3_level_start");

            controller.DebugStartLevel(0);
            yield return new WaitForSecondsRealtime(3f);
            yield return Capture("4_game");

            controller.DebugShowEndScreen(true, 2);
            yield return new WaitForSecondsRealtime(2.8f);
            yield return Capture("5_win");

            controller.DebugShowEndScreen(false, 0);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Capture("6_lose");

            // Obstacles and shaped boards.
            controller.ShowMap();
            controller.ShowLevelStart(29);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Capture("7_level30_popup");

            controller.DebugStartLevel(25);
            yield return new WaitForSecondsRealtime(3f);
            yield return Capture("8_level26_game");

            controller.DebugStartLevel(29);
            yield return new WaitForSecondsRealtime(3f);
            yield return Capture("9_level30_game");

            // The surprise only, the camera is not opened (it would switch on the PC webcam).
            controller.DebugShowFinalSurprise();
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Capture("10_final_surprise");

            Debug.Log($"[Flowers] Screenshot tour done: {OutputFolder}");
            EditorApplication.isPlaying = false;
        }

        private static IEnumerator Capture(string name)
        {
            string path = Path.Combine(OutputFolder, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            // The file is written at the end of the frame; give it a couple of frames.
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }
}
#endif
