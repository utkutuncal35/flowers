using UnityEngine;

namespace Flowers.Core
{
    /// <summary>
    /// Plays sounds and vibration from anywhere: <c>FeedbackPlayer.Play(SoundId.Pop)</c>.
    /// One instance lives in the scene; if it's missing, calls simply do nothing.
    /// Uses a few AudioSources in turn so overlapping sounds (pop + praise) don't cut each other off.
    /// </summary>
    public class FeedbackPlayer : MonoBehaviour
    {
        [SerializeField] private JuiceConfig config;
        [Tooltip("How many sounds can play at the same time.")]
        [SerializeField, Range(2, 16)] private int voices = 8;

        private static FeedbackPlayer _instance;

        private AudioSource[] _sources;
        private int _nextSource;

        public static JuiceConfig Config => _instance != null ? _instance.config : null;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError($"[Flowers] FeedbackPlayer on '{name}': Config is not assigned. Run Flowers → Setup Scene.", this);
                enabled = false;
                return;
            }

            _instance = this;
            _sources = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _sources[i] = source;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public static void Play(SoundId id, float pitch = 1f)
        {
            if (_instance != null && _instance.enabled) _instance.PlaySound(id, pitch);
        }

        public static void Vibrate(HapticStrength strength)
        {
            if (_instance == null || !_instance.enabled || !_instance.config.VibrationEnabled) return;
            _instance.config.GetVibration(strength, out int milliseconds, out int amplitude);
            Haptics.Vibrate(milliseconds, amplitude);
        }

        private void PlaySound(SoundId id, float pitch)
        {
            if (!config.TryGetSound(id, out var clip, out float volume)) return;

            var source = _sources[_nextSource];
            _nextSource = (_nextSource + 1) % _sources.Length;
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
        }
    }
}
