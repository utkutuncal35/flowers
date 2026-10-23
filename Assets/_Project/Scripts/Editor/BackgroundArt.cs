using System;
using C = Flowers.Editor.Raster.Color;

namespace Flowers.Editor
{
    /// <summary>
    /// Backgrounds and decorations drawn in code: a romantic garden for the game screen, a tileable meadow and a
    /// sky cap for the level map, plus a ribbon banner, light rays and the map marker.
    /// All return RGBA bytes bottom-up (Unity order). Pure C# so they can be previewed outside Unity.
    /// Random layouts use fixed seeds, so every run produces the same picture.
    /// </summary>
    public static class BackgroundArt
    {
        // Palette
        private static readonly C SkyPink = C.Hex(0xF6BFCD);
        private static readonly C SkyPeach = C.Hex(0xFCE0DC);
        private static readonly C SkyCream = C.Hex(0xFFF3EA);
        private static readonly C White = C.Hex(0xFFFFFF);
        private static readonly C HillFar = C.Hex(0xE8BFC8);
        private static readonly C HillMid = C.Hex(0xBFD9A8);
        private static readonly C HillNear = C.Hex(0x9CC783);
        private static readonly C Grass = C.Hex(0x86B96E);
        private static readonly C GrassDark = C.Hex(0x6E9E58);
        private static readonly C Leaf = C.Hex(0x5E8C4A);
        private static readonly C LeafDark = C.Hex(0x4A7539);
        private static readonly C Bordo = C.Hex(0x8E2C48);
        private static readonly C BordoDark = C.Hex(0x5A1028);
        private static readonly C Cream = C.Hex(0xFFF7EE);
        private static readonly C Petal = C.Hex(0xF7A9C3);

        // ---------- Game screen ----------

        /// <summary>Portrait garden scene: pink sky, sun glow, clouds, layered hills, flower bushes in front.</summary>
        public static byte[] Garden(int width, int height)
        {
            var r = new Raster(width, height);
            r.VerticalGradient(new[] { 0f, 0.45f, 0.66f }, new[] { SkyPink, SkyPeach, SkyCream });

            r.SoftCircle(width * 0.76f, height * 0.16f, width * 0.42f, White, 0.55f);
            r.FillCircle(width * 0.76f, height * 0.16f, width * 0.07f, C.Hex(0xFFF8EE), 0.9f);

            Cloud(r, width * 0.22f, height * 0.12f, width * 0.2f, 0.9f);
            Cloud(r, width * 0.62f, height * 0.29f, width * 0.15f, 0.75f);
            Cloud(r, width * 0.95f, height * 0.06f, width * 0.12f, 0.7f);

            var random = new Random(7);
            for (int i = 0; i < 14; i++)
                FloatingPetal(r, (float)random.NextDouble() * width, height * (0.05f + (float)random.NextDouble() * 0.5f),
                    width * 0.012f, (float)random.NextDouble() * 180f, 0.55f);

            Hills(r, height * 0.66f, height * 0.035f, 0.009f, 1.3f, HillFar, 1f);
            Hills(r, height * 0.74f, height * 0.03f, 0.012f, 0.2f, HillMid, 1f);
            Hills(r, height * 0.83f, height * 0.025f, 0.016f, 2.1f, HillNear, 1f);
            Hills(r, height * 0.9f, height * 0.015f, 0.022f, 0.7f, Grass, 1f);

            // Row of flower bushes along the bottom edge.
            var flowers = FlowerStamps(64);
            for (int i = 0; i < 9; i++)
            {
                float x = width * (i / 8f) + (float)(random.NextDouble() - 0.5) * width * 0.06f;
                float y = height * (0.93f + (float)random.NextDouble() * 0.04f);
                Bush(r, x, y, width * (0.09f + (float)random.NextDouble() * 0.03f), random, flowers, 3);
            }
            return r.ToUnityBytes();
        }

        // ---------- Level map ----------

        /// <summary>
        /// Meadow that tiles vertically (top edge continues the bottom edge): grass with soft patches,
        /// tufts, small flower clusters and bushes, kept mostly to the sides so the path stays clear.
        /// </summary>
        public static byte[] MapMeadow(int width, int height)
        {
            var r = new Raster(width, height);
            var random = new Random(11);

            // Base: grass with a slow, vertically periodic light variation so the tile has no visible seam.
            r.Fill(0, 0, width, height, (x, y) => true, (x, y) =>
            {
                float wave = (float)(Math.Sin(y / height * Math.PI * 2 * 2 + x * 0.01) * 0.5 + 0.5);
                return C.Lerp(C.Hex(0xA8D08C), C.Hex(0xB6D99B), wave * 0.7f);
            });

            // Everything below is drawn with vertical wrap-around so it tiles.
            void Wrapped(float y, Action<float> draw)
            {
                draw(y);
                draw(y - height);
                draw(y + height);
            }

            for (int i = 0; i < 26; i++)
            {
                float x = (float)random.NextDouble() * width, y = (float)random.NextDouble() * height;
                float rx = width * (0.08f + (float)random.NextDouble() * 0.1f);
                Wrapped(y, yy => r.FillEllipse(x, yy, rx, rx * 0.55f, C.Hex(0xC4E0AA), 0.55f));
            }

            for (int i = 0; i < 140; i++)
            {
                float x = (float)random.NextDouble() * width, y = (float)random.NextDouble() * height;
                Wrapped(y, yy => Tuft(r, x, yy, width * 0.018f));
            }

            var flowers = FlowerStamps(48);
            for (int i = 0; i < 16; i++)
            {
                float x = Side(random, width), y = (float)random.NextDouble() * height;
                Wrapped(y, yy => FlowerCluster(r, x, yy, width * 0.05f, flowers, random.Next()));
            }

            for (int i = 0; i < 8; i++)
            {
                float x = Side(random, width), y = (float)random.NextDouble() * height;
                int seed = random.Next();
                Wrapped(y, yy => Bush(r, x, yy, width * 0.075f, new Random(seed), flowers, 2));
            }

            for (int i = 0; i < 14; i++)
            {
                float x = (float)random.NextDouble() * width, y = (float)random.NextDouble() * height;
                float s = width * (0.008f + (float)random.NextDouble() * 0.01f);
                Wrapped(y, yy =>
                {
                    r.FillEllipse(x, yy + s * 0.3f, s * 1.3f, s * 0.8f, C.Hex(0x8AAE74), 0.5f);
                    r.FillEllipse(x, yy, s * 1.2f, s * 0.75f, C.Hex(0xE9E2D6), 1f);
                });
            }
            return r.ToUnityBytes();
        }

        /// <summary>Sky with clouds and hills for the top of the map; its bottom row matches the meadow color.</summary>
        public static byte[] MapSkyCap(int width, int height)
        {
            var r = new Raster(width, height);
            r.VerticalGradient(new[] { 0f, 0.5f, 0.7f }, new[] { SkyPink, SkyPeach, SkyCream });
            r.SoftCircle(width * 0.25f, height * 0.25f, width * 0.4f, White, 0.5f);
            Cloud(r, width * 0.7f, height * 0.18f, width * 0.17f, 0.9f);
            Cloud(r, width * 0.18f, height * 0.38f, width * 0.13f, 0.8f);
            Hills(r, height * 0.62f, height * 0.05f, 0.011f, 0.4f, HillFar, 1f);
            Hills(r, height * 0.74f, height * 0.04f, 0.015f, 1.7f, HillMid, 1f);
            Hills(r, height * 0.86f, height * 0.03f, 0.02f, 2.6f, C.Hex(0xA8D08C), 1f);
            // The bottom of the cap fades to transparent, so it melts into the meadow tile drawn under it.
            var bytes = r.ToUnityBytes();
            int fadeRows = (int)(height * 0.22f);
            for (int row = 0; row < fadeRows; row++) // row 0 = bottom row (Unity order)
            {
                float alpha = Raster.Smooth(row / (float)fadeRows);
                for (int x = 0; x < width; x++)
                {
                    int i = (row * width + x) * 4 + 3;
                    bytes[i] = (byte)(bytes[i] * alpha);
                }
            }
            return bytes;
        }

        // ---------- UI decorations ----------

        /// <summary>
        /// Bordo ribbon banner: a band with folded tails at both ends. 9-slice it horizontally
        /// (left/right borders cover the tails) so it stretches to any title.
        /// </summary>
        public static byte[] Ribbon(int width, int height)
        {
            var r = new Raster(width, height);
            float bandTop = height * 0.12f, bandBottom = height * 0.78f;
            float tailTop = height * 0.34f, tailBottom = height * 0.94f;
            float tailW = width * 0.13f, fold = width * 0.04f;

            foreach (int side in new[] { -1, 1 })
            {
                float outer = side < 0 ? 0f : width;
                float inner = side < 0 ? tailW + fold : width - tailW - fold;
                // Tail with a V notch at the outer end.
                r.Fill(Math.Min(outer, inner), tailTop, Math.Max(outer, inner), tailBottom, (x, y) =>
                {
                    if (y < tailTop || y > tailBottom) return false;
                    float distFromOuter = Math.Abs(x - outer);
                    float mid = (tailTop + tailBottom) * 0.5f;
                    float notch = tailW * 0.35f * (1f - Math.Abs(y - mid) / ((tailBottom - tailTop) * 0.5f));
                    return distFromOuter > notch && distFromOuter < tailW + fold;
                }, (x, y) => C.Lerp(BordoDark, Bordo, 0.35f));
                // Small dark fold triangle where the tail tucks behind the band.
                float fx = side < 0 ? tailW + fold : width - tailW - fold;
                r.Fill(fx - fold, bandBottom - 2, fx + fold, tailBottom, (x, y) =>
                {
                    float t = (y - bandBottom) / (tailBottom - bandBottom);
                    float edge = side < 0 ? fx - fold * (1 - t) : fx + fold * (1 - t);
                    return y >= bandBottom && y <= tailBottom - (tailBottom - bandBottom) * 0.25f &&
                           (side < 0 ? x >= edge && x <= fx + 1 : x <= edge && x >= fx - 1);
                }, (x, y) => BordoDark);
            }

            float left = tailW, right = width - tailW;
            r.Fill(left, bandTop, right, bandBottom, (x, y) => y >= bandTop && y <= bandBottom && x >= left && x <= right,
                (x, y) =>
                {
                    float t = (y - bandTop) / (bandBottom - bandTop);
                    var c = C.Lerp(C.Hex(0xB8456A), Bordo, Raster.Smooth(t * 1.4f));
                    if (t > 0.86f) c = C.Lerp(c, BordoDark, 0.5f);           // bottom shade
                    if (t > 0.08f && t < 0.16f) c = C.Lerp(c, C.Hex(0xE7A0B8), 0.35f); // stitched highlight line
                    return c;
                });
            return r.ToUnityBytes();
        }

        /// <summary>White light rays fading outwards; tinted and slowly rotated behind the win stars.</summary>
        public static byte[] Rays(int size)
        {
            var r = new Raster(size, size);
            float c = size * 0.5f;
            const int rays = 14;
            r.Fill(0, 0, size, size, (x, y) =>
            {
                float dx = x - c, dy = y - c;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d > c) return false;
                double a = Math.Atan2(dy, dx);
                return Math.Sin(a * rays) > 0.25;
            }, (x, y) => White);
            // Fade with distance: multiply alpha by redrawing a radial mask is simpler as a separate pass.
            var bytes = r.ToUnityBytes();
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = (float)Math.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c)) / c;
                    float fade = Raster.Smooth(1f - d) * Raster.Smooth(d * 4f);
                    int i = (y * size + x) * 4 + 3;
                    bytes[i] = (byte)(bytes[i] * fade);
                }
            return bytes;
        }

        /// <summary>Heart-shaped pin that bounces above the current level on the map.</summary>
        public static byte[] MapMarker(int size)
        {
            var r = new Raster(size, size);
            float s = size;
            Func<float, float, float, bool> heart = (x, y, grow) =>
            {
                float hx = (x - s * 0.5f) / (s * (0.3f + grow));
                float hy = -(y - s * 0.52f) / (s * (0.3f + grow));
                float k = hx * hx + hy * hy - 1f;
                return k * k * k - hx * hx * hy * hy * hy < 0f;
            };
            // Soft shadow, cream outline, red heart, shine.
            r.Fill(0, 0, s, s, (x, y) => heart(x, y - s * 0.04f, 0.06f), (x, y) => BordoDark, 0.3f);
            r.Fill(0, 0, s, s, (x, y) => heart(x, y, 0.06f), (x, y) => Cream);
            r.Fill(0, 0, s, s, (x, y) => heart(x, y, 0f), (x, y) =>
                C.Lerp(C.Hex(0xFF5A75), C.Hex(0xC8143A), Raster.Smooth((y - s * 0.15f) / (s * 0.7f))));
            r.FillEllipse(s * 0.38f, s * 0.4f, s * 0.07f, s * 0.045f, White, 0.7f);
            return r.ToUnityBytes();
        }

        // ---------- Pieces ----------

        private static void Cloud(Raster r, float cx, float cy, float size, float opacity)
        {
            var c = White;
            r.FillEllipse(cx, cy + size * 0.18f, size * 1.15f, size * 0.32f, c, opacity);
            r.FillCircle(cx - size * 0.5f, cy + size * 0.05f, size * 0.38f, c, opacity);
            r.FillCircle(cx, cy - size * 0.12f, size * 0.52f, c, opacity);
            r.FillCircle(cx + size * 0.55f, cy + size * 0.02f, size * 0.4f, c, opacity);
        }

        private static void Hills(Raster r, float baseY, float amplitude, float frequency, float phase, C color, float opacity)
        {
            r.Fill(0, baseY - amplitude * 2, r.Width, r.Height, (x, y) =>
                y > baseY + amplitude * (float)(Math.Sin(x * frequency + phase) + 0.45 * Math.Sin(x * frequency * 2.3 + phase * 1.7)),
                (x, y) => C.Lerp(color, C.Lerp(color, White, 0.25f), Raster.Smooth((baseY + amplitude * 3 - y) / (amplitude * 6))), opacity);
        }

        private static void Bush(Raster r, float cx, float cy, float size, Random random, Flower[] flowers, int flowerCount)
        {
            r.FillEllipse(cx, cy + size * 0.35f, size * 1.2f, size * 0.35f, LeafDark, 0.35f);
            r.FillCircle(cx - size * 0.55f, cy + size * 0.1f, size * 0.55f, LeafDark);
            r.FillCircle(cx + size * 0.55f, cy + size * 0.1f, size * 0.55f, LeafDark);
            r.FillCircle(cx, cy - size * 0.2f, size * 0.7f, Leaf);
            r.FillCircle(cx - size * 0.5f, cy, size * 0.5f, Leaf);
            r.FillCircle(cx + size * 0.5f, cy, size * 0.5f, Leaf);
            r.FillCircle(cx - size * 0.15f, cy - size * 0.4f, size * 0.3f, C.Hex(0x76A65E), 0.8f);
            for (int i = 0; i < flowerCount; i++)
            {
                var f = flowers[random.Next(flowers.Length)];
                float fx = cx + (float)(random.NextDouble() - 0.5) * size * 1.4f;
                float fy = cy - size * 0.1f + (float)(random.NextDouble() - 0.5) * size * 0.8f;
                r.Stamp(f.Pixels, f.Size, f.Size, fx, fy, size * 0.75f, 1f, (float)(random.NextDouble() - 0.5) * 40f);
            }
        }

        private static void FlowerCluster(Raster r, float cx, float cy, float size, Flower[] flowers, int seed)
        {
            var random = new Random(seed);
            for (int k = 0; k < 3; k++)
            {
                float lx = cx + (float)(random.NextDouble() - 0.5) * size * 1.6f;
                float ly = cy + (float)(random.NextDouble() - 0.5) * size;
                r.FillEllipse(lx + size * 0.25f, ly + size * 0.3f, size * 0.3f, size * 0.14f, Leaf);
                var f = flowers[random.Next(flowers.Length)];
                r.Stamp(f.Pixels, f.Size, f.Size, lx, ly, size * (0.8f + (float)random.NextDouble() * 0.4f));
            }
        }

        private static void Tuft(Raster r, float cx, float cy, float size)
        {
            for (int k = -1; k <= 1; k++)
            {
                float tipX = cx + k * size * 0.5f, tipY = cy - size * (k == 0 ? 1.1f : 0.8f);
                r.Fill(cx - size, tipY, cx + size, cy, (x, y) =>
                {
                    if (y < tipY || y > cy) return false;
                    float t = (y - tipY) / (cy - tipY);
                    float centerX = tipX + (cx + k * size * 0.2f - tipX) * t;
                    return Math.Abs(x - centerX) < size * 0.14f * t;
                }, (x, y) => GrassDark, 0.8f);
            }
        }

        private static void FloatingPetal(Raster r, float cx, float cy, float size, float angleDegrees, float opacity)
        {
            double a = angleDegrees * Math.PI / 180;
            float cos = (float)Math.Cos(a), sin = (float)Math.Sin(a);
            r.Fill(cx - size * 2, cy - size * 2, cx + size * 2, cy + size * 2, (x, y) =>
            {
                float dx = x - cx, dy = y - cy;
                float lx = dx * cos + dy * sin, ly = -dx * sin + dy * cos;
                return lx * lx / (size * size * 2.2f) + ly * ly / (size * size * 0.7f) < 1f;
            }, (x, y) => Petal, opacity);
        }

        /// <summary>Random x biased towards the left/right thirds (the map path runs through the middle).</summary>
        private static float Side(Random random, float width)
        {
            float t = (float)random.NextDouble() * 0.36f;
            return random.Next(2) == 0 ? width * (0.02f + t) : width * (0.98f - t);
        }

        private readonly struct Flower
        {
            public readonly byte[] Pixels;
            public readonly int Size;
            public Flower(byte[] pixels, int size) { Pixels = pixels; Size = size; }
        }

        private static Flower[] FlowerStamps(int size)
        {
            var result = new Flower[FlowerArt.TypeCount];
            for (int i = 0; i < result.Length; i++)
                result[i] = new Flower(FlowerArt.Render(i, size), size);
            return result;
        }
    }
}
