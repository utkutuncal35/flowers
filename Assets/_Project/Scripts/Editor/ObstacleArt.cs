using System;
using C = Flowers.Editor.Raster.Color;

namespace Flowers.Editor
{
    /// <summary>
    /// Obstacle sprites drawn in code, in the same style as the flowers (thin bordo outline, soft shading):
    /// a clay pot with a little sprout (plus a cracked version for 2-layer pots), a vine wreath that sits
    /// on top of a flower, and a soil tile. RGBA bytes bottom-up (Unity order). Pure C# for previews.
    /// </summary>
    public static class ObstacleArt
    {
        private static readonly C Outline = C.Hex(0x4A0D22);
        private static readonly C ClayDark = C.Hex(0x9C4E3E);
        private static readonly C Clay = C.Hex(0xC66B55);
        private static readonly C ClayLight = C.Hex(0xE39A80);
        private static readonly C Leaf = C.Hex(0x5E8C4A);
        private static readonly C LeafLight = C.Hex(0x86B26A);
        private static readonly C LeafDark = C.Hex(0x2F5A2A);
        private static readonly C SoilDark = C.Hex(0x7A4A32);
        private static readonly C Soil = C.Hex(0x9C6444);
        private static readonly C SoilLight = C.Hex(0xB57B58);
        private static readonly C White = C.Hex(0xFFFFFF);

        /// <summary>Clay pot with a sprout. cracked = the second layer of a 2-layer pot is already broken.</summary>
        public static byte[] Pot(int size, bool cracked)
        {
            var r = new Raster(size, size);
            float s = size;
            float rimTop = s * 0.30f, rimBottom = s * 0.44f, bodyBottom = s * 0.88f;
            float rimHalf = s * 0.40f, topHalf = s * 0.34f, bottomHalf = s * 0.24f;

            // Sprout (behind the rim).
            r.Fill(s * 0.46f, s * 0.08f, s * 0.54f, rimTop + 2, (x, y) => Math.Abs(x - s * 0.5f) < s * 0.025f && y > s * 0.14f, (x, y) => Leaf);
            LeafShape(r, s * 0.41f, s * 0.15f, s * 0.11f, -35f);
            LeafShape(r, s * 0.6f, s * 0.11f, s * 0.12f, 30f);

            Func<float, float, float, bool> body = (x, y, grow) =>
            {
                if (y < rimBottom - grow || y > bodyBottom + grow) return false;
                float t = (y - rimBottom) / (bodyBottom - rimBottom);
                float half = topHalf + (bottomHalf - topHalf) * t + grow;
                return Math.Abs(x - s * 0.5f) < half;
            };
            Func<float, float, float, bool> rim = (x, y, grow) =>
                y > rimTop - grow && y < rimBottom + grow && Math.Abs(x - s * 0.5f) < rimHalf + grow;

            float o = s * 0.03f;
            r.Fill(0, 0, s, s, (x, y) => body(x, y - s * 0.03f, o) || rim(x, y - s * 0.03f, o), (x, y) => Outline, 0.25f); // shadow
            r.Fill(0, 0, s, s, (x, y) => body(x, y, o) || rim(x, y, o), (x, y) => Outline);
            r.Fill(0, 0, s, s, (x, y) => body(x, y, 0), (x, y) =>
            {
                float side = (x - s * 0.5f) / topHalf; // -1..1
                return C.Lerp(C.Lerp(Clay, ClayLight, Raster.Smooth(-side * 0.8f)), ClayDark, Raster.Smooth(side * 1.2f - 0.2f) * 0.6f);
            });
            r.Fill(0, 0, s, s, (x, y) => rim(x, y, 0), (x, y) =>
                C.Lerp(ClayLight, Clay, Raster.Smooth((y - rimTop) / (rimBottom - rimTop))));
            // Rim shadow line on the body and a highlight stripe.
            r.Fill(0, 0, s, s, (x, y) => body(x, y, 0) && y < rimBottom + s * 0.03f, (x, y) => ClayDark, 0.5f);
            r.Fill(0, 0, s, s, (x, y) => body(x, y, 0) && Math.Abs(x - s * 0.36f) < s * 0.03f && y > rimBottom + s * 0.06f && y < bodyBottom - s * 0.08f,
                (x, y) => White, 0.3f);

            if (cracked)
            {
                Crack(r, new[] { (0.56f, 0.46f), (0.5f, 0.56f), (0.58f, 0.64f), (0.52f, 0.74f) }, s);
                Crack(r, new[] { (0.36f, 0.5f), (0.42f, 0.6f), (0.38f, 0.68f) }, s);
            }
            return r.ToUnityBytes();
        }

        /// <summary>
        /// Wreath of two twisted green strands with leaves, around the edge of a cell. The middle is clear so the
        /// wrapped flower shows through; drawn on top of it.
        /// </summary>
        public static byte[] Vine(int size)
        {
            var r = new Raster(size, size);
            float c = size * 0.5f, radius = size * 0.4f, band = size * 0.035f;

            for (int strand = 0; strand < 2; strand++)
            {
                float phase = strand * (float)Math.PI;
                r.Fill(0, 0, size, size, (x, y) =>
                {
                    float dx = x - c, dy = y - c;
                    float d = (float)Math.Sqrt(dx * dx + dy * dy);
                    float a = (float)Math.Atan2(dy, dx);
                    float wave = radius + (float)Math.Sin(a * 6 + phase) * size * 0.035f;
                    return Math.Abs(d - wave) < band + size * 0.012f;
                }, (x, y) => LeafDark);
                r.Fill(0, 0, size, size, (x, y) =>
                {
                    float dx = x - c, dy = y - c;
                    float d = (float)Math.Sqrt(dx * dx + dy * dy);
                    float a = (float)Math.Atan2(dy, dx);
                    float wave = radius + (float)Math.Sin(a * 6 + phase) * size * 0.035f;
                    return Math.Abs(d - wave) < band;
                }, (x, y) => strand == 0 ? Leaf : LeafLight);
            }

            for (int k = 0; k < 6; k++)
            {
                double a = k * Math.PI / 3 + 0.3;
                float lx = c + (float)Math.Cos(a) * radius, ly = c + (float)Math.Sin(a) * radius;
                LeafShape(r, lx, ly, size * 0.11f, (float)(a * 180 / Math.PI) + 90f);
            }
            return r.ToUnityBytes();
        }

        /// <summary>Soil tile: rounded brown square with darker specks (tiled under flowers, not tinted).</summary>
        public static byte[] SoilTile(int size)
        {
            var r = new Raster(size, size);
            float s = size, radius = s * 0.14f, inset = s * 0.03f;
            Func<float, float, bool> inside = (x, y) =>
            {
                float qx = Math.Abs(x - s * 0.5f) - (s * 0.5f - inset - radius);
                float qy = Math.Abs(y - s * 0.5f) - (s * 0.5f - inset - radius);
                if (qx <= 0 || qy <= 0) return Math.Abs(x - s * 0.5f) <= s * 0.5f - inset && Math.Abs(y - s * 0.5f) <= s * 0.5f - inset;
                return qx * qx + qy * qy <= radius * radius;
            };
            r.Fill(0, 0, s, s, inside, (x, y) => C.Lerp(SoilLight, Soil, Raster.Smooth(y / s)));

            var random = new Random(3);
            for (int i = 0; i < 26; i++)
            {
                float x = s * (0.15f + (float)random.NextDouble() * 0.7f), y = s * (0.15f + (float)random.NextDouble() * 0.7f);
                float rad = s * (0.015f + (float)random.NextDouble() * 0.025f);
                r.FillEllipse(x, y, rad * 1.4f, rad, i % 3 == 0 ? SoilLight : SoilDark, 0.8f);
            }
            return r.ToUnityBytes();
        }

        // ---------- Pieces ----------

        private static void LeafShape(Raster r, float cx, float cy, float length, float angleDegrees)
        {
            double a = angleDegrees * Math.PI / 180;
            float cos = (float)Math.Cos(a), sin = (float)Math.Sin(a);
            Func<float, float, float, bool> leaf = (x, y, grow) =>
            {
                float dx = x - cx, dy = y - cy;
                float lx = dx * cos + dy * sin, ly = -dx * sin + dy * cos; // lx along the leaf
                float t = lx / (length + grow);
                if (t < -1 || t > 1) return false;
                float half = (length * 0.42f + grow) * (1 - t * t);
                return Math.Abs(ly) < half;
            };
            r.Fill(cx - length * 1.3f, cy - length * 1.3f, cx + length * 1.3f, cy + length * 1.3f, (x, y) => leaf(x, y, length * 0.12f), (x, y) => LeafDark);
            r.Fill(cx - length * 1.3f, cy - length * 1.3f, cx + length * 1.3f, cy + length * 1.3f, (x, y) => leaf(x, y, 0), (x, y) =>
            {
                float dx = x - cx, dy = y - cy;
                float ly = -dx * sin + dy * cos;
                return ly < 0 ? LeafLight : Leaf;
            });
        }

        private static void Crack(Raster r, (float x, float y)[] points, float s)
        {
            float w = s * 0.018f;
            for (int i = 0; i < points.Length - 1; i++)
            {
                float ax = points[i].x * s, ay = points[i].y * s, bx = points[i + 1].x * s, by = points[i + 1].y * s;
                r.Fill(Math.Min(ax, bx) - w * 2, Math.Min(ay, by) - w * 2, Math.Max(ax, bx) + w * 2, Math.Max(ay, by) + w * 2, (x, y) =>
                {
                    float dx = bx - ax, dy = by - ay;
                    float t = ((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy);
                    t = t < 0 ? 0 : t > 1 ? 1 : t;
                    float px = ax + t * dx - x, py = ay + t * dy - y;
                    return px * px + py * py < w * w;
                }, (x, y) => Outline);
            }
        }
    }
}
