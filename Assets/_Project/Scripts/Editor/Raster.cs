using System;

namespace Flowers.Editor
{
    /// <summary>
    /// A tiny software canvas for drawing backgrounds and UI decorations in code.
    /// Float RGBA, origin at the TOP-left while drawing (like an image editor); <see cref="ToUnityBytes"/>
    /// flips it to Unity's bottom-up order. Shapes are anti-aliased with 4x4 supersampling at their edges.
    /// Pure C# so the results can be previewed outside Unity.
    /// </summary>
    public sealed class Raster
    {
        public readonly int Width;
        public readonly int Height;
        private readonly float[] _r, _g, _b, _a;

        public Raster(int width, int height)
        {
            Width = width;
            Height = height;
            _r = new float[width * height];
            _g = new float[width * height];
            _b = new float[width * height];
            _a = new float[width * height];
        }

        // ---------- Colors ----------

        public readonly struct Color
        {
            public readonly float R, G, B;
            public Color(float r, float g, float b) { R = r; G = g; B = b; }
            public static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
            public static Color Lerp(Color a, Color b, float t) =>
                new Color(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);
        }

        // ---------- Drawing ----------

        /// <summary>Fills every pixel with a vertical gradient (stops are 0..1 from top to bottom).</summary>
        public void VerticalGradient(float[] stops, Color[] colors)
        {
            for (int y = 0; y < Height; y++)
            {
                float t = (y + 0.5f) / Height;
                var c = Sample(stops, colors, t);
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    _r[i] = c.R; _g[i] = c.G; _b[i] = c.B; _a[i] = 1f;
                }
            }
        }

        /// <summary>
        /// Paints a shape given as "inside(x, y)" in pixel coordinates. color(x, y) gives the color at each point
        /// (for gradients/details). Only the bounding box is visited.
        /// </summary>
        public void Fill(float minX, float minY, float maxX, float maxY, Func<float, float, bool> inside,
            Func<float, float, Color> color, float opacity = 1f)
        {
            int x0 = Math.Max(0, (int)Math.Floor(minX)), y0 = Math.Max(0, (int)Math.Floor(minY));
            int x1 = Math.Min(Width - 1, (int)Math.Ceiling(maxX)), y1 = Math.Min(Height - 1, (int)Math.Ceiling(maxY));
            const int ss = 4;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    // Quick test at the 4 corners + center; only supersample pixels on an edge.
                    bool c0 = inside(x, y), c1 = inside(x + 1, y), c2 = inside(x, y + 1), c3 = inside(x + 1, y + 1);
                    bool cc = inside(x + 0.5f, y + 0.5f);
                    float coverage;
                    if (c0 && c1 && c2 && c3 && cc) coverage = 1f;
                    else if (!c0 && !c1 && !c2 && !c3 && !cc) continue;
                    else
                    {
                        int hits = 0;
                        for (int sy = 0; sy < ss; sy++)
                            for (int sx = 0; sx < ss; sx++)
                                if (inside(x + (sx + 0.5f) / ss, y + (sy + 0.5f) / ss)) hits++;
                        if (hits == 0) continue;
                        coverage = hits / (float)(ss * ss);
                    }
                    Blend(x, y, color(x + 0.5f, y + 0.5f), coverage * opacity);
                }
            }
        }

        public void FillCircle(float cx, float cy, float radius, Color color, float opacity = 1f) =>
            Fill(cx - radius, cy - radius, cx + radius, cy + radius,
                (x, y) => (x - cx) * (x - cx) + (y - cy) * (y - cy) < radius * radius, (x, y) => color, opacity);

        public void FillEllipse(float cx, float cy, float rx, float ry, Color color, float opacity = 1f) =>
            Fill(cx - rx, cy - ry, cx + rx, cy + ry,
                (x, y) => (x - cx) * (x - cx) / (rx * rx) + (y - cy) * (y - cy) / (ry * ry) < 1f, (x, y) => color, opacity);

        /// <summary>Soft round blob that fades out towards its edge (for glows and soft shadows).</summary>
        public void SoftCircle(float cx, float cy, float radius, Color color, float opacity)
        {
            int x0 = Math.Max(0, (int)(cx - radius)), x1 = Math.Min(Width - 1, (int)(cx + radius));
            int y0 = Math.Max(0, (int)(cy - radius)), y1 = Math.Min(Height - 1, (int)(cy + radius));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = (float)Math.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy)) / radius;
                    if (d >= 1f) continue;
                    float t = 1f - d;
                    Blend(x, y, color, t * t * opacity);
                }
        }

        /// <summary>
        /// Stamps an RGBA image (bottom-up rows, as produced for Unity) centered at (cx, cy), scaled to the given width.
        /// Bilinear sampling. Optional tint multiplies the image color.
        /// </summary>
        public void Stamp(byte[] rgbaBottomUp, int srcW, int srcH, float cx, float cy, float width, float opacity = 1f, float rotationDegrees = 0f)
        {
            float scale = width / srcW;
            float height = srcH * scale;
            float half = Math.Max(width, height) * 0.75f;
            double rad = -rotationDegrees * Math.PI / 180.0;
            float cos = (float)Math.Cos(rad), sin = (float)Math.Sin(rad);

            int x0 = Math.Max(0, (int)(cx - half)), x1 = Math.Min(Width - 1, (int)(cx + half));
            int y0 = Math.Max(0, (int)(cy - half)), y1 = Math.Min(Height - 1, (int)(cy + half));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float lx = dx * cos - dy * sin, ly = dx * sin + dy * cos;
                    float u = lx / scale + srcW * 0.5f;
                    float v = srcH * 0.5f - ly / scale; // source rows are bottom-up
                    if (u < 0 || v < 0 || u >= srcW - 1 || v >= srcH - 1) continue;

                    int iu = (int)u, iv = (int)v;
                    float fu = u - iu, fv = v - iv;
                    float r = 0, g = 0, b = 0, a = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        int su = iu + (k & 1), sv = iv + (k >> 1);
                        float w = ((k & 1) == 1 ? fu : 1 - fu) * ((k >> 1) == 1 ? fv : 1 - fv);
                        int si = (sv * srcW + su) * 4;
                        float sa = rgbaBottomUp[si + 3] / 255f * w;
                        r += rgbaBottomUp[si] / 255f * sa; g += rgbaBottomUp[si + 1] / 255f * sa; b += rgbaBottomUp[si + 2] / 255f * sa;
                        a += sa;
                    }
                    if (a <= 0.001f) continue;
                    Blend(x, y, new Color(r / a, g / a, b / a), a * opacity);
                }
            }
        }

        /// <summary>Porter-Duff "over" of a color with the given alpha onto pixel (x, y).</summary>
        public void Blend(int x, int y, Color c, float alpha)
        {
            if (alpha <= 0f || x < 0 || y < 0 || x >= Width || y >= Height) return;
            if (alpha > 1f) alpha = 1f;
            int i = y * Width + x;
            float outA = alpha + _a[i] * (1f - alpha);
            if (outA <= 0f) return;
            float wb = _a[i] * (1f - alpha) / outA, wt = alpha / outA;
            _r[i] = c.R * wt + _r[i] * wb;
            _g[i] = c.G * wt + _g[i] * wb;
            _b[i] = c.B * wt + _b[i] * wb;
            _a[i] = outA;
        }

        // ---------- Output ----------

        /// <summary>RGBA bytes with the bottom row first (what Texture2D.SetPixelData expects).</summary>
        public byte[] ToUnityBytes()
        {
            var bytes = new byte[Width * Height * 4];
            for (int y = 0; y < Height; y++)
            {
                int dst = (Height - 1 - y) * Width * 4;
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x, d = dst + x * 4;
                    bytes[d] = ToByte(_r[i]); bytes[d + 1] = ToByte(_g[i]); bytes[d + 2] = ToByte(_b[i]); bytes[d + 3] = ToByte(_a[i]);
                }
            }
            return bytes;
        }

        // ---------- Helpers ----------

        public static Color Sample(float[] stops, Color[] colors, float t)
        {
            if (t <= stops[0]) return colors[0];
            for (int k = 1; k < stops.Length; k++)
                if (t <= stops[k])
                    return Color.Lerp(colors[k - 1], colors[k], Smooth((t - stops[k - 1]) / (stops[k] - stops[k - 1])));
            return colors[colors.Length - 1];
        }

        public static float Smooth(float t)
        {
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            return t * t * (3f - 2f * t);
        }

        private static byte ToByte(float v) => (byte)(Math.Max(0f, Math.Min(1f, v)) * 255f + 0.5f);
    }
}
