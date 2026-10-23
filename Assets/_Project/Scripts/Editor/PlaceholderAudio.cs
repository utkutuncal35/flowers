using System;
using System.Collections.Generic;
using System.IO;
using Flowers.Core;
using UnityEditor;
using UnityEngine;

namespace Flowers.Editor
{
    /// <summary>Writes the synthesized placeholder sounds as WAV files and imports them as short, mono clips.</summary>
    public static class PlaceholderAudio
    {
        public const string Folder = "Assets/_Project/Audio/Placeholder";

        private static readonly (SoundId id, Func<float[]> make)[] Sounds =
        {
            (SoundId.Pop, PlaceholderSounds.Pop),
            (SoundId.Swap, PlaceholderSounds.Swap),
            (SoundId.Invalid, PlaceholderSounds.Invalid),
            (SoundId.Click, PlaceholderSounds.Click),
            (SoundId.Star, PlaceholderSounds.Star),
            (SoundId.Praise, PlaceholderSounds.Praise),
            (SoundId.Win, PlaceholderSounds.Win),
            (SoundId.Lose, PlaceholderSounds.Lose),
        };

        /// <summary>Default volume per sound, used only the first time a sound is added to JuiceConfig.</summary>
        public static float DefaultVolume(SoundId id)
        {
            switch (id)
            {
                case SoundId.Swap: return 0.5f;
                case SoundId.Click: return 0.5f;
                case SoundId.Invalid: return 0.7f;
                case SoundId.Praise: return 0.7f;
                case SoundId.Lose: return 0.7f;
                default: return 0.9f;
            }
        }

        public static Dictionary<SoundId, AudioClip> GenerateAll()
        {
            EditorFolders.Ensure(Folder);
            var clips = new Dictionary<SoundId, AudioClip>();
            foreach (var (id, make) in Sounds)
            {
                string path = $"{Folder}/{id}.wav";
                File.WriteAllBytes(path, PlaceholderSounds.ToWav(make()));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                importer.forceToMono = true;
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad; // short clips: decode once, play instantly
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();

                clips[id] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            return clips;
        }
    }
}
