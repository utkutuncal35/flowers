using System;
using System.IO;
using System.Text;

namespace Flowers.Editor
{
    /// <summary>
    /// Synthesizes the temporary sound effects (mono, 44.1 kHz) so there is no license to worry about.
    /// Pure C#: every sound is a few sine waves with soft envelopes, aiming for "sweet", not "arcade".
    /// Replace with real recordings later by dropping new clips into JuiceConfig.
    /// </summary>
    public static class PlaceholderSounds
    {
        public const int SampleRate = 44100;
        private const float Peak = 0.8f; // leave headroom so overlapping sounds don't clip as easily

        /// <summary>Soft "bloop": a sine that slides down in pitch, like a bubble popping.</summary>
        public static float[] Pop()
        {
            var s = Buffer(0.18f);
            double phase = 0;
            for (int i = 0; i < s.Length; i++)
            {
                float t = Time(i);
                float freq = 520f + 480f * (float)Math.Exp(-t * 28f);
                phase += 2 * Math.PI * freq / SampleRate;
                float env = Attack(t, 0.004f) * (float)Math.Exp(-t * 22f);
                s[i] = env * (float)(Math.Sin(phase) + 0.25 * Math.Sin(2 * phase));
            }
            return Normalize(s, Peak);
        }

        /// <summary>Quiet upward slide for a swap.</summary>
        public static float[] Swap()
        {
            var s = Buffer(0.14f);
            double phase = 0;
            for (int i = 0; i < s.Length; i++)
            {
                float t = Time(i);
                float freq = 330f + 260f * (t / 0.14f);
                phase += 2 * Math.PI * freq / SampleRate;
                float env = Attack(t, 0.01f) * (float)Math.Exp(-t * 18f);
                s[i] = env * (float)Math.Sin(phase);
            }
            return Normalize(s, 0.45f);
        }

        /// <summary>Gentle low "boing" with a pitch wobble for an invalid swap.</summary>
        public static float[] Invalid()
        {
            var s = Buffer(0.3f);
            double phase = 0;
            for (int i = 0; i < s.Length; i++)
            {
                float t = Time(i);
                float freq = 210f * (1f + 0.12f * (float)Math.Sin(t * 2 * Math.PI * 14f) * (float)Math.Exp(-t * 8f)) - 40f * t;
                phase += 2 * Math.PI * freq / SampleRate;
                float env = Attack(t, 0.006f) * (float)Math.Exp(-t * 11f);
                s[i] = env * (float)(Math.Sin(phase) + 0.3 * Math.Sin(2 * phase));
            }
            return Normalize(s, 0.6f);
        }

        /// <summary>Very short soft tick for UI buttons.</summary>
        public static float[] Click()
        {
            var s = Buffer(0.06f);
            for (int i = 0; i < s.Length; i++)
            {
                float t = Time(i);
                float env = Attack(t, 0.002f) * (float)Math.Exp(-t * 90f);
                s[i] = env * (float)Math.Sin(2 * Math.PI * 1300f * t);
            }
            return Normalize(s, 0.4f);
        }

        /// <summary>Bell chime for a star (the game raises its pitch for each star).</summary>
        public static float[] Star() => Normalize(Bell(1046.5f, 0.9f), Peak);

        /// <summary>Little sparkle for praise text: two quick high bell notes.</summary>
        public static float[] Praise()
        {
            var s = Buffer(0.7f);
            Mix(s, Bell(1568f, 0.6f), 0f, 0.6f);   // G6
            Mix(s, Bell(2093f, 0.6f), 0.08f, 0.6f); // C7
            return Normalize(s, 0.6f);
        }

        /// <summary>Happy rising arpeggio C5 E5 G5 C6 E6.</summary>
        public static float[] Win()
        {
            var s = Buffer(1.6f);
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f };
            for (int n = 0; n < notes.Length; n++)
                Mix(s, Bell(notes[n], 1.0f), n * 0.11f, n == notes.Length - 1 ? 1f : 0.75f);
            return Normalize(s, Peak);
        }

        /// <summary>Soft, not sad: two warm notes going gently down (G4 → E4).</summary>
        public static float[] Lose()
        {
            var s = Buffer(1.1f);
            Mix(s, Warm(392f, 0.6f), 0f, 1f);
            Mix(s, Warm(329.6f, 0.8f), 0.28f, 1f);
            return Normalize(s, 0.6f);
        }

        // ---------- Building blocks ----------

        /// <summary>Bell-ish tone: a fundamental plus two inharmonic partials that die out faster.</summary>
        private static float[] Bell(float freq, float duration)
        {
            var s = Buffer(duration);
            for (int i = 0; i < s.Length; i++)
            {
                float t = Time(i);
                float a = Attack(t, 0.003f);
                s[i] = a * (float)(
                    Math.Sin(2 * Math.PI * freq * t) * Math.Exp(-t * 5f) +
                    0.35 * Math.Sin(2 * Math.PI * freq * 2.76 * t) * Math.Exp(-t * 9f) +
                    0.15 * Math.Sin(2 * Math.PI * freq * 5.4 * t) * Math.Exp(-t * 14f));
            }
            return s;
        }

        /// <summary>Soft rounded tone (sine + a bit of 2nd harmonic), slow attack.</summary>
        private static float[] Warm(float freq, float duration)
        {
            var s = Buffer(duration);
            for (int i = 0; i < s.Length; i++)
            {
                float t = Time(i);
                float env = Attack(t, 0.03f) * (float)Math.Exp(-t * 4f);
                s[i] = env * (float)(Math.Sin(2 * Math.PI * freq * t) + 0.2 * Math.Sin(4 * Math.PI * freq * t));
            }
            return s;
        }

        private static float[] Buffer(float seconds) => new float[(int)(seconds * SampleRate)];

        private static float Time(int sample) => sample / (float)SampleRate;

        /// <summary>Linear fade-in, avoids clicks at the start.</summary>
        private static float Attack(float t, float length) => t < length ? t / length : 1f;

        private static void Mix(float[] target, float[] source, float startSeconds, float gain)
        {
            int offset = (int)(startSeconds * SampleRate);
            for (int i = 0; i < source.Length && offset + i < target.Length; i++)
                target[offset + i] += source[i] * gain;
        }

        private static float[] Normalize(float[] s, float peak)
        {
            float max = 0f;
            foreach (var v in s) max = Math.Max(max, Math.Abs(v));
            if (max <= 0f) return s;

            float gain = peak / max;
            for (int i = 0; i < s.Length; i++) s[i] *= gain;

            // Short fade-out so no sound ends with a click.
            int fade = Math.Min(s.Length, SampleRate / 100);
            for (int i = 0; i < fade; i++) s[s.Length - 1 - i] *= i / (float)fade;
            return s;
        }

        // ---------- WAV ----------

        /// <summary>16-bit PCM mono WAV bytes.</summary>
        public static byte[] ToWav(float[] samples)
        {
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream))
            {
                int dataBytes = samples.Length * 2;
                w.Write(Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + dataBytes);
                w.Write(Encoding.ASCII.GetBytes("WAVE"));
                w.Write(Encoding.ASCII.GetBytes("fmt "));
                w.Write(16);
                w.Write((short)1);              // PCM
                w.Write((short)1);              // mono
                w.Write(SampleRate);
                w.Write(SampleRate * 2);        // byte rate
                w.Write((short)2);              // block align
                w.Write((short)16);             // bits
                w.Write(Encoding.ASCII.GetBytes("data"));
                w.Write(dataBytes);
                foreach (var v in samples)
                    w.Write((short)(Math.Max(-1f, Math.Min(1f, v)) * short.MaxValue));
                w.Flush();
                return stream.ToArray();
            }
        }
    }
}
