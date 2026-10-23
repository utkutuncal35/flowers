using System;
using UnityEngine;

namespace Flowers.Core
{
    public enum SoundId
    {
        Pop,
        Swap,
        Invalid,
        Click,
        Star,
        Praise,
        Win,
        Lose,
    }

    public enum HapticStrength
    {
        Light,
        Medium,
    }

    /// <summary>
    /// Sounds, vibration, petals and praise words: all the "feel" tuning in one asset. Edit the asset, not the code.
    /// </summary>
    [CreateAssetMenu(menuName = "Flowers/Juice Config", fileName = "JuiceConfig")]
    public class JuiceConfig : ScriptableObject
    {
        [Serializable]
        public class SoundEntry
        {
            public SoundId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        [Header("Sound")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.8f;
        [SerializeField] private SoundEntry[] sounds;
        [Tooltip("Each chain step plays the pop this many semitones higher (2 = a whole tone).")]
        [SerializeField, Range(0f, 6f)] private float comboSemitonesPerStep = 2f;
        [SerializeField, Range(1f, 3f)] private float maxComboPitch = 2f;

        [Header("Vibration")]
        [SerializeField] private bool vibrationEnabled = true;
        [SerializeField, Range(5, 100)] private int lightMilliseconds = 18;
        [SerializeField, Range(1, 255)] private int lightAmplitude = 70;
        [SerializeField, Range(5, 200)] private int mediumMilliseconds = 45;
        [SerializeField, Range(1, 255)] private int mediumAmplitude = 150;

        [Header("Petals (when flowers pop)")]
        [Tooltip("Petal color per flower type, index = type id.")]
        [SerializeField] private Color[] petalColors =
        {
            new Color32(0x66, 0x12, 0x2E, 0xFF),
            new Color32(0xE8, 0x34, 0x4A, 0xFF),
            new Color32(0xEC, 0x5F, 0x95, 0xFF),
            new Color32(0xFF, 0xF3, 0xDC, 0xFF),
            new Color32(0xF7, 0xA9, 0xC3, 0xFF),
        };
        [SerializeField, Range(0, 20)] private int petalsPerFlower = 6;
        [SerializeField] private Vector2 petalSpeed = new Vector2(2.5f, 5f);
        [SerializeField] private Vector2 petalSize = new Vector2(0.18f, 0.32f);
        [SerializeField] private Vector2 petalLifetime = new Vector2(0.5f, 0.9f);

        [Header("Win celebration")]
        [SerializeField, Range(0, 300)] private int confettiCount = 90;
        [SerializeField] private Vector2 confettiLifetime = new Vector2(2.2f, 3.2f);
        [SerializeField] private Vector2 confettiSize = new Vector2(0.22f, 0.4f);

        [Header("Praise (big chains)")]
        [Tooltip("Show praise from this chain length on (3 = the swap's match plus two chain reactions).")]
        [SerializeField, Range(2, 10)] private int praiseMinChain = 3;
        [Tooltip("First word for the shortest praised chain, then one word per extra step.")]
        [SerializeField] private string[] praiseWords = { "Güzel!", "Harika!", "Muhteşem!", "İnanılmaz!" };

        private AudioClip[] _clipById;
        private float[] _volumeById;

        public float MasterVolume => masterVolume;
        public bool VibrationEnabled => vibrationEnabled;
        public int PetalsPerFlower => petalsPerFlower;
        public Vector2 PetalSpeed => petalSpeed;
        public Vector2 PetalSize => petalSize;
        public Vector2 PetalLifetime => petalLifetime;
        public int ConfettiCount => confettiCount;
        public Vector2 ConfettiLifetime => confettiLifetime;
        public Vector2 ConfettiSize => confettiSize;
        public int PetalColorCount => petalColors == null ? 0 : petalColors.Length;

        public Color PetalColor(int flowerType) =>
            petalColors != null && flowerType >= 0 && flowerType < petalColors.Length ? petalColors[flowerType] : Color.white;

        public bool TryGetSound(SoundId id, out AudioClip clip, out float volume)
        {
            if (_clipById == null) BuildLookup();
            clip = _clipById[(int)id];
            volume = _volumeById[(int)id] * masterVolume;
            return clip != null;
        }

        /// <summary>Pitch for the pop of chain step <paramref name="stepIndex"/> (0 = normal pitch).</summary>
        public float ComboPitch(int stepIndex) =>
            Mathf.Min(maxComboPitch, Mathf.Pow(2f, stepIndex * comboSemitonesPerStep / 12f));

        public void GetVibration(HapticStrength strength, out int milliseconds, out int amplitude)
        {
            bool light = strength == HapticStrength.Light;
            milliseconds = light ? lightMilliseconds : mediumMilliseconds;
            amplitude = light ? lightAmplitude : mediumAmplitude;
        }

        /// <summary>Null if no praise for this chain length.</summary>
        public string PraiseFor(int chainLength)
        {
            if (praiseWords == null || praiseWords.Length == 0 || chainLength < praiseMinChain) return null;
            return praiseWords[Mathf.Min(chainLength - praiseMinChain, praiseWords.Length - 1)];
        }

        private void OnValidate() => _clipById = null; // rebuild after Inspector edits

        private void BuildLookup()
        {
            int count = Enum.GetValues(typeof(SoundId)).Length;
            _clipById = new AudioClip[count];
            _volumeById = new float[count];
            if (sounds == null) return;
            foreach (var entry in sounds)
            {
                if (entry == null) continue;
                _clipById[(int)entry.id] = entry.clip;
                _volumeById[(int)entry.id] = entry.volume;
            }
        }

#if UNITY_EDITOR
        /// <summary>Used by Flowers → Setup Scene. Keeps volumes already set for sounds that exist.</summary>
        public void EditorAssignClip(SoundId id, AudioClip clip, float defaultVolume)
        {
            var list = new System.Collections.Generic.List<SoundEntry>(sounds ?? new SoundEntry[0]);
            var entry = list.Find(e => e != null && e.id == id);
            if (entry == null)
            {
                entry = new SoundEntry { id = id, volume = defaultVolume };
                list.Add(entry);
            }
            entry.clip = clip;
            sounds = list.ToArray();
            _clipById = null;
        }
#endif
    }
}
