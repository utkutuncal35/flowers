using System;

namespace Flowers.Editor
{
    /// <summary>
    /// Simple white shapes drawn as raw RGBA pixels (bottom row first, like Unity textures), tinted in the game:
    /// board tiles and frame, the map's dots and lock, the goal check mark and the petal particle.
    /// The flowers themselves are drawn by <see cref="FlowerArt"/>; buttons, panels and stars come from Kenney.
    /// Pure C# so it can be checked outside Unity.
    /// </summary>
    public static class PlaceholderShapes
    {
        /// <summary>White rounded square, used for tiles and (9-sliced) for the board frame.</summary>
        public static byte[] RenderRoundedSquare(int size, float cornerRadius) =>
            RenderWhite(size, (u, v) => InsideRoundedSquare(u, v, 1f, cornerRadius));

        /// <summary>White check mark (tinted in the UI).</summary>
        public static byte[] RenderCheck(int size) =>
            RenderWhite(size, (u, v) =>
                DistanceToSegment(u, v, -0.62f, 0.02f, -0.18f, -0.45f) < 0.15f ||
                DistanceToSegment(u, v, -0.18f, -0.45f, 0.68f, 0.55f) < 0.15f);

        /// <summary>White single petal (round end, pointed tip) for the particle effects.</summary>
        public static byte[] RenderPetal(int size) =>
            RenderWhite(size, (u, v) => Petal(u, v, 0f, -0.15f, 0f, 0.5f, 0.75f, 0.9f));

        /// <summary>White circle: the map's dotted path.</summary>
        public static byte[] RenderCircle(int size) =>
            RenderWhite(size, (u, v) => u * u + v * v < 0.96f * 0.96f);

        /// <summary>White padlock (tinted in the UI).</summary>
        public static byte[] RenderLock(int size) =>
            RenderWhite(size, (u, v) =>
            {
                bool body = InsideRoundedSquare(u / 0.62f, (v + 0.32f) / 0.5f, 1f, 0.25f);
                float dy = v - 0.15f;
                float r = (float)Math.Sqrt(u * u + dy * dy);
                bool shackle = dy >= 0f && r > 0.26f && r < 0.42f;
                bool legs = dy < 0f && v > -0.1f && Math.Abs(u) > 0.26f && Math.Abs(u) < 0.42f;
                return body || shackle || legs;
            });

        public static byte[] RenderSolidSquare(int size)
        {
            var pixels = new byte[size * size * 4];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = 255;
            return pixels;
        }

        // ---------- Helpers ----------

        /// <summary>White shape with 2x2 supersampled edges; alpha = coverage.</summary>
        private static byte[] RenderWhite(int size, Func<float, float, bool> inside)
        {
            var pixels = new byte[size * size * 4];
            for (int py = 0; py < size; py++)
                for (int px = 0; px < size; px++)
                {
                    float coverage = 0f;
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                        {
                            float u = (px + 0.25f + 0.5f * sx) / size * 2f - 1f;
                            float v = (py + 0.25f + 0.5f * sy) / size * 2f - 1f;
                            if (inside(u, v)) coverage += 0.25f;
                        }
                    if (coverage <= 0f) continue;
                    int i = (py * size + px) * 4;
                    pixels[i] = pixels[i + 1] = pixels[i + 2] = 255;
                    pixels[i + 3] = (byte)(coverage * 255f + 0.5f);
                }
            return pixels;
        }

        private static bool InsideRoundedSquare(float u, float v, float half, float radius)
        {
            float qx = Math.Abs(u) - (half - radius);
            float qy = Math.Abs(v) - (half - radius);
            if (qx <= 0f || qy <= 0f) return Math.Abs(u) <= half && Math.Abs(v) <= half;
            return qx * qx + qy * qy <= radius * radius;
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

        private static float DistanceToSegment(float u, float v, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay;
            float t = ((u - ax) * dx + (v - ay) * dy) / (dx * dx + dy * dy);
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            float px = ax + t * dx - u, py = ay + t * dy - v;
            return (float)Math.Sqrt(px * px + py * py);
        }
    }
}
