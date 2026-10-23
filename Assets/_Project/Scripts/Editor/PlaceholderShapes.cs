using System;

namespace Flowers.Editor
{
    /// <summary>
    /// Draws the temporary flower sprites as raw RGBA pixels (bottom row first, like Unity textures).
    /// Pure C# so it can be checked outside Unity. Each type has its own silhouette AND its own
    /// lightness, so the five types stay readable in grayscale (GDD: "Okunabilirlik").
    /// </summary>
    public static class PlaceholderShapes
    {
        public const int FlowerTypeCount = 5;

        public static readonly string[] FlowerNames = { "Rose", "Tulip", "Blossom", "Daisy", "Heart" };

        // Darkest → lightest. Palette: bordo, cherry red, pink, light pink, cream.
        private static readonly Rgb[] FillColors =
        {
            new Rgb(0x66, 0x12, 0x2E), // 0 Rose     bordo (main accent)
            new Rgb(0xE8, 0x34, 0x4A), // 1 Tulip    cherry red
            new Rgb(0xEC, 0x5F, 0x95), // 2 Blossom  pink
            new Rgb(0xFF, 0xF3, 0xDC), // 3 Daisy    cream
            new Rgb(0xF7, 0xA9, 0xC3), // 4 Heart    light pink
        };

        private static readonly Rgb[] DetailColors =
        {
            new Rgb(0x96, 0x34, 0x55),
            new Rgb(0xB0, 0x1A, 0x33),
            new Rgb(0xA8, 0x2A, 0x5A),
            new Rgb(0xEC, 0x5F, 0x95),
            new Rgb(0xFF, 0xD6, 0xE3),
        };

        private static readonly Rgb Outline = new Rgb(0x3A, 0x0C, 0x1C);
        private const float OutlineWidth = 0.07f;

        private readonly struct Rgb
        {
            public readonly float R, G, B;
            public Rgb(byte r, byte g, byte b) { R = r / 255f; G = g / 255f; B = b / 255f; }
            public Rgb(float r, float g, float b) { R = r; G = g; B = b; }
            public static Rgb Lerp(Rgb a, Rgb b, float t) =>
                new Rgb(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);
        }

        // ---------- Public renderers ----------

        public static byte[] RenderFlower(int type, int size)
        {
            Func<float, float, bool> inside = ShapeFor(type);
            var fill = FillColors[type];
            var detail = DetailColors[type];

            var pixels = new byte[size * size * 4];
            var ringOffsets = MakeRing(OutlineWidth, 16);

            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float fillCoverage = 0f, outerCoverage = 0f;
                    float detailCoverage = 0f;

                    // 2x2 supersampling for smooth edges.
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                        {
                            float u = (px + 0.25f + 0.5f * sx) / size * 2f - 1f;
                            float v = (py + 0.25f + 0.5f * sy) / size * 2f - 1f;

                            bool inFill = inside(u, v);
                            bool inOuter = inFill || InsideAny(inside, u, v, ringOffsets);
                            if (inFill) fillCoverage += 0.25f;
                            if (inOuter) outerCoverage += 0.25f;
                            if (inFill && IsDetail(type, u, v)) detailCoverage += 0.25f;
                        }

                    int i = (py * size + px) * 4;
                    if (outerCoverage <= 0f) continue;

                    float v0 = (py + 0.5f) / size * 2f - 1f;
                    // Soft top-light so the flowers don't look flat.
                    var body = Rgb.Lerp(fill, Brighten(fill, 0.18f), Clamp01(v0 * 0.5f + 0.5f));
                    if (fillCoverage > 0f)
                        body = Rgb.Lerp(body, detail, detailCoverage / fillCoverage);

                    var color = Rgb.Lerp(Outline, body, fillCoverage / outerCoverage);
                    Write(pixels, i, color, outerCoverage);
                }
            }
            return pixels;
        }

        /// <summary>White rounded square, used for tiles and (9-sliced) for the board frame.</summary>
        public static byte[] RenderRoundedSquare(int size, float cornerRadius)
        {
            var pixels = new byte[size * size * 4];
            var white = new Rgb(1f, 1f, 1f);
            for (int py = 0; py < size; py++)
                for (int px = 0; px < size; px++)
                {
                    float coverage = 0f;
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                        {
                            float u = (px + 0.25f + 0.5f * sx) / size * 2f - 1f;
                            float v = (py + 0.25f + 0.5f * sy) / size * 2f - 1f;
                            if (InsideRoundedSquare(u, v, 1f, cornerRadius)) coverage += 0.25f;
                        }
                    if (coverage > 0f) Write(pixels, (py * size + px) * 4, white, coverage);
                }
            return pixels;
        }

        public static byte[] RenderSolidSquare(int size)
        {
            var pixels = new byte[size * size * 4];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = 255;
            return pixels;
        }

        // ---------- Shapes (u, v in -1..1, v up) ----------

        private static Func<float, float, bool> ShapeFor(int type)
        {
            switch (type)
            {
                case 0: return Rose;
                case 1: return Tulip;
                case 2: return Blossom;
                case 3: return Daisy;
                case 4: return Heart;
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        // Round, gently scalloped.
        private static bool Rose(float u, float v)
        {
            Polar(u, v, out float r, out float a);
            return r < 0.76f + 0.04f * (float)Math.Cos(7f * a);
        }

        // Three upward-pointing petals: a tall middle one and two leaning outwards.
        private static bool Tulip(float u, float v)
        {
            return Petal(u, v, 0f, -0.14f, 0f, 0.36f, 0.52f, 0.86f)
                || Petal(u, v, -0.2f, -0.2f, 0.38f, 0.3f, 0.45f, 0.72f)
                || Petal(u, v, 0.2f, -0.2f, -0.38f, 0.3f, 0.45f, 0.72f);
        }

        /// <summary>A drop shape: round bottom (half-ellipse), pointed tip at the top. Rotated by angle (radians, CCW).</summary>
        private static bool Petal(float u, float v, float cx, float cy, float angle,
            float halfWidth, float bottomHeight, float topHeight)
        {
            float dx = u - cx, dy = v - cy;
            float cos = (float)Math.Cos(-angle), sin = (float)Math.Sin(-angle);
            float x = dx * cos - dy * sin;
            float y = dx * sin + dy * cos;

            if (y <= 0f)
                return (x * x) / (halfWidth * halfWidth) + (y * y) / (bottomHeight * bottomHeight) < 1f;
            if (y >= topHeight) return false;
            float t = y / topHeight;
            return Math.Abs(x) < halfWidth * (1f - t * t);
        }

        // Five wide round petals.
        private static bool Blossom(float u, float v)
        {
            Polar(u, v, out float r, out float a);
            float petal = (float)Math.Pow(Math.Abs(Math.Cos(2.5f * (a - Math.PI / 2))), 0.8f);
            return r < 0.42f + 0.44f * petal;
        }

        // Many thin petals.
        private static bool Daisy(float u, float v)
        {
            Polar(u, v, out float r, out float a);
            float petal = (float)Math.Pow(Math.Abs(Math.Cos(5f * a)), 2.5f);
            return r < 0.34f + 0.54f * petal;
        }

        private static bool Heart(float u, float v)
        {
            float x = u / 0.68f;
            float y = (v + 0.08f) / 0.68f;
            float k = x * x + y * y - 1f;
            return k * k * k - x * x * y * y * y < 0f;
        }

        private static bool IsDetail(int type, float u, float v)
        {
            Polar(u, v, out float r, out _);
            switch (type)
            {
                case 0: return Math.Abs(r - 0.48f) < 0.035f || Math.Abs(r - 0.22f) < 0.035f;
                case 1: return !Petal(u, v, 0f, -0.14f, 0f, 0.36f, 0.52f, 0.86f); // side petals a shade darker
                case 2: return r < 0.15f;
                case 3: return r < 0.24f;
                case 4: return Math.Abs(u + 0.28f) < 0.12f && Math.Abs(v - 0.28f) < 0.08f; // little shine
                default: return false;
            }
        }

        // ---------- Helpers ----------

        private static bool InsideRoundedSquare(float u, float v, float half, float radius)
        {
            float qx = Math.Abs(u) - (half - radius);
            float qy = Math.Abs(v) - (half - radius);
            if (qx <= 0f || qy <= 0f) return Math.Abs(u) <= half && Math.Abs(v) <= half;
            return qx * qx + qy * qy <= radius * radius;
        }

        private static float[] MakeRing(float radius, int count)
        {
            var offsets = new float[count * 2];
            for (int k = 0; k < count; k++)
            {
                double angle = k * 2.0 * Math.PI / count;
                offsets[k * 2] = radius * (float)Math.Cos(angle);
                offsets[k * 2 + 1] = radius * (float)Math.Sin(angle);
            }
            return offsets;
        }

        private static bool InsideAny(Func<float, float, bool> inside, float u, float v, float[] offsets)
        {
            for (int k = 0; k < offsets.Length; k += 2)
                if (inside(u + offsets[k], v + offsets[k + 1]))
                    return true;
            return false;
        }

        private static void Polar(float u, float v, out float r, out float angle)
        {
            r = (float)Math.Sqrt(u * u + v * v);
            angle = (float)Math.Atan2(v, u);
        }

        private static Rgb Brighten(Rgb c, float amount) =>
            new Rgb(c.R + (1f - c.R) * amount, c.G + (1f - c.G) * amount, c.B + (1f - c.B) * amount);

        private static float Clamp01(float x) => x < 0f ? 0f : x > 1f ? 1f : x;

        private static void Write(byte[] pixels, int i, Rgb c, float alpha)
        {
            pixels[i] = (byte)(Clamp01(c.R) * 255f + 0.5f);
            pixels[i + 1] = (byte)(Clamp01(c.G) * 255f + 0.5f);
            pixels[i + 2] = (byte)(Clamp01(c.B) * 255f + 0.5f);
            pixels[i + 3] = (byte)(Clamp01(alpha) * 255f + 0.5f);
        }
    }
}
