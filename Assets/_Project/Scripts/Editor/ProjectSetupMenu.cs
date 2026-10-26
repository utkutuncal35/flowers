using UnityEditor;
using UnityEngine;

namespace Flowers.Editor
{
    /// <summary>
    /// One-click project configuration. Safe to run multiple times.
    /// </summary>
    public static class ProjectSetupMenu
    {
        [MenuItem("Flowers/Project Setup")]
        public static void RunProjectSetup()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                if (!switched)
                {
                    Debug.LogError("[Flowers] Could not switch to Android. Is the Android Build Support module installed (Unity Hub → Installs → Add modules)?");
                    return;
                }
            }

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            AppIconMenu.MakeAppIcon();

            AssetDatabase.SaveAssets();
            Debug.Log("[Flowers] Project Setup done: platform = Android, default orientation = Portrait, app icon set.");
        }
    }
}
