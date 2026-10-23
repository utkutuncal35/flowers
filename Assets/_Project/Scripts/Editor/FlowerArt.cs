using System;

namespace Flowers.Editor
{
    /// <summary>
    /// Draws the five flower sprites as raw RGBA pixels (bottom row first, like Unity textures).
    /// Pure C# so the result can be previewed outside Unity.
    ///
    /// Rules from the GDD: every type has its own silhouette AND its own lightness (readable in grayscale),
    /// colors come from the palette (bordo, cherry red, pink, cream, a little leaf green).
    /// Look: soft drop shadow, thin bordo outline, gentle gradients, a small glossy highlight.
    /// </summary>
    public static class FlowerArt
    {
        public const int TypeCount = 5;
        public static readonly string[] Names = { "Rose", "Tulip", "Blossom", "Daisy", "Heart" };

        private const float OutlineWidth = 0.045f;
        private const float ShadowOffset = 0.06f;
        private const float ShadowAlpha = 0.22f;
        private const float HighlightAlpha = 0.28f;

        private static readonly Rgb Outline = Rgb.Hex(0x4A0D22);
        private static readonly Rgb Leaf = Rgb.Hex(0x5E8C4A);
        private static readonly Rgb LeafLight = Rgb.Hex(0x86B26A);
        private static readonly Rgb White = new Rgb(1f, 1f, 1f);

        // ---------- Public ----------

        public static byte[] Render(int type, int size)
        {
            Func<float, float, bool> inside = ShapeOf(type);
            var ring = MakeRing(OutlineWidth, 16);
            var pixels = new byte[size * size * 4];
            const int ss = 3; // 3x3 supersampling for smooth edges and fine details
            const float sampleWeight = 1f / (ss * ss);

            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float fill = 0f, outer = 0f, shadow = 0f;
                    float r = 0f, g = 0f, b = 0f;

                    for (int sy = 0; sy < ss; sy++)
                    {
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float u = (px + (sx + 0.5f) / ss) / size * 2f - 1f;
                            float v = (py + (sy + 0.5f) / ss) / size * 2f - 1f;

                            if (inside(u, v))
                            {
                                fill += sampleWeight;
                                outer += sampleWeight;
                                var c = Shade(type, u, v);
                                c = AddHighlight(c, u, v);
                                r += c.R * sampleWeight; g += c.G * sampleWeight; b += c.B * sampleWeight;
                            }
                            else if (InsideAny(inside, u, v, ring))
                            {
                                outer += sampleWeight;
                            }

                            float su = u, sv = v + ShadowOffset;
                            if (inside(su, sv) || InsideAny(inside, su, sv, ring))
                                shadow += sampleWeight;
                        }
                    }

                    // Composite back to front: shadow, outline, fill.
                    var color = Outline;
                    float alpha = shadow * ShadowAlpha;
                    color = Over(color, alpha, Outline, outer, out alpha);
                    if (fill > 0f)
                        color = Over(color, alpha, new Rgb(r / fill, g / fill, b / fill), fill, out alpha);

                    Write(pixels, (py * size + px) * 4, color, alpha);
                }
            }
            return pixels;
        }

        // ---------- Shapes (u, v in -1..1, v up) ----------

        private static Func<float, float, bool> ShapeOf(int type)
        {
            switch (type)
            {
                case 0: return RoseShape;
                case 1: return TulipShape;
                case 2: return BlossomShape;
                case 3: return DaisyShape;
                case 4: return HeartShape;
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private static float RoseRadius(float a) => 0.74f + 0.05f * (float)Math.Cos(7f * a);

        private static bool RoseShape(float u, float v)
        {
            Polar(u, v + 0.03f, out float r, out float a);
            return r < RoseRadius(a);
        }

        // Tulip drawn in its own space, then shrunk a little and lifted to make room for the stem.
        private const float TulipScale = 0.86f;
        private const float TulipLift = 0.1f;

        private static void TulipSpace(float u, float v, out float x, out float y)
        {
            x = u / TulipScale;
            y = (v - TulipLift) / TulipScale;
        }

        private static bool TulipCenterPetal(float x, float y) => Petal(x, y, 0f, -0.14f, 0f, 0.36f, 0.52f, 0.86f);
        private static bool TulipLeftPetal(float x, float y) => Petal(x, y, -0.2f, -0.2f, 0.38f, 0.3f, 0.45f, 0.72f);
        private static bool TulipRightPetal(float x, float y) => Petal(x, y, 0.2f, -0.2f, -0.38f, 0.3f, 0.45f, 0.72f);

        private static bool TulipStem(float u, float v) => Math.Abs(u) < 0.045f && v < -0.35f && v > -0.9f;

        private static bool TulipLeaf(float u, float v) => Petal(u, v, 0.16f, -0.8f, -0.75f, 0.11f, 0.12f, 0.3f);

        private static bool TulipShape(float u, float v)
        {
            TulipSpace(u, v, out float x, out float y);
            return TulipCenterPetal(x, y) || TulipLeftPetal(x, y) || TulipRightPetal(x, y)
                || TulipStem(u, v) || TulipLeaf(u, v);
        }

        private static float BlossomRadius(float a, out float petalCenterDistance)
        {
            // Five round petals, each with a small notch at its tip (like a cherry blossom).
            float local = a - (float)Math.PI / 2;
            float c = (float)Math.Pow(Math.Abs(Math.Cos(2.5f * local)), 0.8f);
            float sector = (float)(2 * Math.PI / 5);
            float d = (float)Math.IEEERemainder(local, sector);
            petalCenterDistance = Math.Abs(d);
            float notch = 0.08f * (float)Math.Exp(-(d * d) / (0.06f * 0.06f));
            return 0.42f + 0.44f * c - notch;
        }

        private static bool BlossomShape(float u, float v)
        {
            Polar(u, v, out float r, out float a);
            return r < BlossomRadius(a, out _);
        }

        private static float DaisyRadius(float a) => 0.34f + 0.52f * (float)Math.Pow(Math.Abs(Math.Cos(5f * a)), 2.5f);

        private static bool DaisyShape(float u, float v)
        {
            Polar(u, v, out float r, out float a);
            return r < DaisyRadius(a);
        }

        private static bool HeartShape(float u, float v)
        {
            float x = u / 0.68f;
            float y = (v + 0.06f) / 0.68f;
            float k = x * x + y * y - 1f;
            return k * k * k - x * x * y * y * y < 0f;
        }

        // ---------- Shading ----------

        private static Rgb Shade(int type, float u, float v)
        {
            switch (type)
            {
                case 0: return ShadeRose(u, v);
                case 1: return ShadeTulip(u, v);
                case 2: return ShadeBlossom(u, v);
                case 3: return ShadeDaisy(u, v);
                default: return ShadeHeart(u, v);
            }
        }

        // Bordo rose seen from above: petals spiral in towards a dark center.
        private static Rgb ShadeRose(float u, float v)
        {
            var dark = Rgb.Hex(0x3E0A1C);
            var mid = Rgb.Hex(0x66122E);
            var light = Rgb.Hex(0xA03458);

            Polar(u, v + 0.03f, out float r, out float a);
            float rn = r / RoseRadius(a);
            float spiral = Frac(a / (float)(2 * Math.PI) + rn * 2.4f);
            // Each spiral band is darker where it tucks under the next one and lighter at its edge.
            var band = Rgb.Lerp(dark, light, Smooth(0.05f, 0.95f, spiral));
            var c = Rgb.Lerp(band, mid, 0.35f);
            c = Rgb.Lerp(c, light, Smooth(0.75f, 1f, rn) * 0.35f); // lighter outer rim
            return Rgb.Lerp(dark, c, Smooth(0.08f, 0.22f, rn));      // dark heart
        }

        // Cherry-red tulip: middle petal lighter at the top, side petals a shade darker. Green stem and leaf.
        private static Rgb ShadeTulip(float u, float v)
        {
            if (TulipStem(u, v) || TulipLeaf(u, v))
            {
                TulipSpace(u, v, out float sx, out float sy);
                if (!(TulipCenterPetal(sx, sy) || TulipLeftPetal(sx, sy) || TulipRightPetal(sx, sy)))
                    return Rgb.Lerp(Leaf, LeafLight, Smooth(-0.9f, -0.5f, v) * 0.6f + (u > 0.05f ? 0.2f : 0f));
            }

            var dark = Rgb.Hex(0xB01A33);
            var mid = Rgb.Hex(0xE8344A);
            var light = Rgb.Hex(0xFF7484);

            TulipSpace(u, v, out float x, out float y);
            float t = Smooth(-0.6f, 0.7f, y);
            if (TulipCenterPetal(x, y))
            {
                var c = Rgb.Lerp(mid, light, t * 0.8f);
                // Faint line down the middle of the petal.
                if (Math.Abs(x) < 0.02f && y > -0.3f && y < 0.5f) c = Rgb.Lerp(c, dark, 0.35f);
                return c;
            }
            return Rgb.Lerp(dark, mid, t * 0.7f);
        }

        // Pink cherry blossom: darker towards the middle, lighter tips, bordo center with cream stamens.
        private static Rgb ShadeBlossom(float u, float v)
        {
            var dark = Rgb.Hex(0xC23C72);
            var mid = Rgb.Hex(0xEC5F95);
            var light = Rgb.Hex(0xFFA9C8);
            var center = Rgb.Hex(0x8E2C48);
            var stamen = Rgb.Hex(0xFFF3DC);

            Polar(u, v, out float r, out float a);
            if (r < 0.13f) return Rgb.Lerp(center, Rgb.Hex(0xB04A68), Smooth(0.13f, 0f, r) * 0.5f);

            for (int k = 0; k < 5; k++)
            {
                double sa = Math.PI / 2 + Math.PI / 5 + k * 2 * Math.PI / 5;
                float dx = u - 0.21f * (float)Math.Cos(sa), dy = v - 0.21f * (float)Math.Sin(sa);
                if (dx * dx + dy * dy < 0.04f * 0.04f) return stamen;
            }

            float radius = BlossomRadius(a, out float fromPetalCenter);
            float rn = r / radius;
            var c = Rgb.Lerp(dark, mid, Smooth(0.15f, 0.55f, rn));
            c = Rgb.Lerp(c, light, Smooth(0.55f, 1f, rn) * 0.75f);
            // Darker seams where two petals meet.
            float seam = Smooth(0.45f, 0.62f, fromPetalCenter);
            return Rgb.Lerp(c, dark, seam * 0.45f * (1f - rn * 0.5f));
        }

        // Cream daisy: thin petals with a faint vein, pink dotted center.
        private static Rgb ShadeDaisy(float u, float v)
        {
            var petalDark = Rgb.Hex(0xEAD2B8);
            var petal = Rgb.Hex(0xFFF3DC);
            var centerDark = Rgb.Hex(0xC23C72);
            var centerMid = Rgb.Hex(0xEC5F95);
            var centerLight = Rgb.Hex(0xFF9EC0);

            Polar(u, v, out float r, out float a);
            if (r < 0.25f)
            {
                // Domed center: lighter top-left, small darker dots for texture.
                float lightAmount = Smooth(0.25f, -0.25f, (u - v) * 0.7f + r);
                var c = Rgb.Lerp(centerMid, centerLight, lightAmount * 0.6f);
                float gx = Frac(u * 14f + 0.5f) - 0.5f, gy = Frac(v * 14f + 0.5f) - 0.5f;
                if (gx * gx + gy * gy < 0.06f) c = Rgb.Lerp(c, centerDark, 0.45f);
                return Rgb.Lerp(c, centerDark, Smooth(0.18f, 0.25f, r) * 0.5f);
            }

            float rn = r / DaisyRadius(a);
            var p = Rgb.Lerp(petalDark, petal, Smooth(0.25f, 0.7f, r));
            float d = (float)Math.IEEERemainder(a, Math.PI / 5); // angle to nearest petal center
            if (Math.Abs(d) * r < 0.012f && rn < 0.85f) p = Rgb.Lerp(p, petalDark, 0.6f);
            return p;
        }

        // Light pink heart: soft vertical gradient with a slight crease at the top.
        private static Rgb ShadeHeart(float u, float v)
        {
            var dark = Rgb.Hex(0xE07FA3);
            var mid = Rgb.Hex(0xF7A9C3);
            var light = Rgb.Hex(0xFFD3E2);

            var c = Rgb.Lerp(dark, mid, Smooth(-0.75f, -0.1f, v));
            c = Rgb.Lerp(c, light, Smooth(0.0f, 0.6f, v - Math.Abs(u) * 0.4f) * 0.7f);
            float crease = Smooth(0.08f, 0f, Math.Abs(u)) * Smooth(0.15f, 0.45f, v);
            return Rgb.Lerp(c, dark, crease * 0.5f);
        }

        /// <summary>Soft white glint at the top-left, shared by all flowers for a glossy, toy-like look.</summary>
        private static Rgb AddHighlight(Rgb c, float u, float v)
        {
            float x = u + 0.3f, y = v - 0.34f;
            float rx = x * 0.866f + y * 0.5f, ry = -x * 0.5f + y * 0.866f; // rotate 30°
            float d = (rx * rx) / (0.17f * 0.17f) + (ry * ry) / (0.09f * 0.09f);
            float t = Smooth(1f, 0.3f, d) * HighlightAlpha;
            return Rgb.Lerp(c, White, t);
        }

        // ---------- Helpers ----------

        /// <summary>A drop: round bottom (half-ellipse), pointed tip at the top. Rotated by angle (radians, CCW).</summary>
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

        private static float Frac(float x) => x - (float)Math.Floor(x);

        /// <summary>Smoothstep from edge0 to edge1 (edge0 may be larger than edge1 to invert).</summary>
        private static float Smooth(float edge0, float edge1, float x)
        {
            float t = (x - edge0) / (edge1 - edge0);
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            return t * t * (3f - 2f * t);
        }

        /// <summary>Porter-Duff "over": paints top (with alpha topAlpha) over base.</summary>
        private static Rgb Over(Rgb baseColor, float baseAlpha, Rgb top, float topAlpha, out float outAlpha)
        {
            outAlpha = topAlpha + baseAlpha * (1f - topAlpha);
            if (outAlpha <= 0f) return top;
            float wb = baseAlpha * (1f - topAlpha) / outAlpha;
            float wt = topAlpha / outAlpha;
            return new Rgb(top.R * wt + baseColor.R * wb, top.G * wt + baseColor.G * wb, top.B * wt + baseColor.B * wb);
        }

        private static void Write(byte[] pixels, int i, Rgb c, float alpha)
        {
            pixels[i] = ToByte(c.R);
            pixels[i + 1] = ToByte(c.G);
            pixels[i + 2] = ToByte(c.B);
            pixels[i + 3] = ToByte(alpha);
        }

        private static byte ToByte(float x) => (byte)(Math.Max(0f, Math.Min(1f, x)) * 255f + 0.5f);

        private readonly struct Rgb
        {
            public readonly float R, G, B;
            public Rgb(float r, float g, float b) { R = r; G = g; B = b; }
            public static Rgb Hex(int rgb) => new Rgb(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
            public static Rgb Lerp(Rgb a, Rgb b, float t) =>
                new Rgb(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);
        }
    }
}
