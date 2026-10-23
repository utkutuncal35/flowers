using UnityEngine;

namespace Flowers.Core
{
    /// <summary>
    /// Short, soft vibrations on Android. Unity's Handheld.Vibrate() is a long strong buzz, so this calls
    /// Android's Vibrator directly with a duration and strength. Does nothing in the editor or on other platforms.
    /// </summary>
    public static class Haptics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static int _sdkLevel;
        private static bool _initialized;
        private static bool _failed;

        public static void Vibrate(int milliseconds, int amplitude)
        {
            if (_failed) return;
            try
            {
                if (!_initialized) Initialize();
                if (_vibrator == null) return;

                if (_sdkLevel >= 26)
                {
                    using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)milliseconds, amplitude))
                        _vibrator.Call("vibrate", effect);
                }
                else
                {
                    _vibrator.Call("vibrate", (long)milliseconds);
                }
            }
            catch (System.Exception e)
            {
                // Never let vibration break the game: turn it off for this session.
                _failed = true;
                Debug.LogWarning($"[Flowers] Vibration disabled: {e.Message}");
            }
        }

        private static void Initialize()
        {
            _initialized = true;
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                _sdkLevel = version.GetStatic<int>("SDK_INT");
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }
#else
        public static void Vibrate(int milliseconds, int amplitude) { }
#endif

        /// <summary>
        /// Never called. Unity only adds the VIBRATE permission to the Android build if a script
        /// references Handheld.Vibrate, so this reference keeps the permission in.
        /// </summary>
        private static void KeepVibratePermission()
        {
#if UNITY_ANDROID
            Handheld.Vibrate();
#endif
        }
    }
}
