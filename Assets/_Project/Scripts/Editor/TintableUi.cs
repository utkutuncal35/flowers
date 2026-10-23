namespace Flowers.Editor
{
    /// <summary>
    /// Turns Kenney's grey UI sprites into pure grayscale with the brightest pixel at white.
    /// In Unity the Image color multiplies the sprite, so the face of a button shows exactly the tint color
    /// and the darker depth/outline become darker shades of that same color: one sprite, any palette color.
    /// Pure C# (works on raw RGBA bytes) so it can be checked outside Unity.
    /// </summary>
    public static class TintableUi
    {
        public static byte[] ToTintable(byte[] rgba)
        {
            float max = 0f;
            for (int i = 0; i < rgba.Length; i += 4)
                if (rgba[i + 3] > 0) max = System.Math.Max(max, Luminance(rgba, i));
            if (max <= 0f) return (byte[])rgba.Clone();

            var result = new byte[rgba.Length];
            for (int i = 0; i < rgba.Length; i += 4)
            {
                float l = Luminance(rgba, i) / max;
                byte g = (byte)(System.Math.Min(1f, l) * 255f + 0.5f);
                result[i] = g;
                result[i + 1] = g;
                result[i + 2] = g;
                result[i + 3] = rgba[i + 3];
            }
            return result;
        }

        private static float Luminance(byte[] p, int i) =>
            (0.2126f * p[i] + 0.7152f * p[i + 1] + 0.0722f * p[i + 2]) / 255f;
    }
}
