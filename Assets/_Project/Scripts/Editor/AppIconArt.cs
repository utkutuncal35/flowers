using System;

namespace Flowers.Editor
{
    /// <summary>
    /// Draws the app icon: a big rose with a little heart flower on a glowing bordo background.
    /// Android "adaptive" icons come in two layers (background + foreground) that the launcher masks into its own
    /// shape (circle, squircle...), cropping the outer edge, so the foreground keeps its art in the middle ~60%.
    /// Older launchers use a ready-made square or round icon instead.
    /// Pure C# so the result can be previewed outside Unity.
    /// </summary>
    public static class AppIconArt
    {
        private static readonly Raster.Color BordoLight = Raster.Color.Hex(0xB0284F);
        private static readonly Raster.Color Bordo = Raster.Color.Hex(0x7A1533);
        private static readonly Raster.Color BordoDark = Raster.Color.Hex(0x4E0B21);
        private static readonly Raster.Color Cream = Raster.Color.Hex(0xFFF3E6);
        private static readonly Raster.Color Pink = Raster.Color.Hex(0xF4A6BD);

        /// <summary>Adaptive background layer: full square, no transparency.</summary>
        public static byte[] Background(int size)
        {
            var r = new Raster(size, size);
            PaintBackground(r, size, (x, y) => true);
            return r.ToUnityBytes();
        }

        /// <summary>Adaptive foreground layer: transparent, art inside the launcher's safe zone.</summary>
        public static byte[] Foreground(int size)
        {
            var r = new Raster(size, size);
            PaintFlowers(r, size, 0.62f);
            return r.ToUnityBytes();
        }

        /// <summary>Icon for launchers without adaptive icons: rounded square, or a circle when round = true.</summary>
        public static byte[] Legacy(int size, bool round)
        {
            var r = new Raster(size, size);
            float corner = round ? size * 0.5f : size * 0.22f;
            PaintBackground(r, size, (x, y) => InsideRoundedSquare(x, y, size, corner));
            PaintFlowers(r, size, round ? 0.86f : 0.9f);
            return r.ToUnityBytes();
        }

        private static void PaintBackground(Raster r, int size, Func<float, float, bool> inside)
        {
            float c = size * 0.5f;
            // Light from slightly above the middle, darker towards the corners.
            r.Fill(0, 0, size, size, inside, (x, y) =>
            {
                float dx = (x - c) / size, dy = (y - size * 0.42f) / size;
                float d = (float)Math.Sqrt(dx * dx + dy * dy) / 0.72f;
                return Raster.Sample(new[] { 0f, 0.5f, 1f }, new[] { BordoLight, Bordo, BordoDark }, d);
            });

            // A few soft pink petals/dots scattered around, kept subtle.
            var dots = new (float x, float y, float s)[]
            {
                (0.16f, 0.2f, 0.035f), (0.84f, 0.17f, 0.028f), (0.12f, 0.78f, 0.025f),
                (0.87f, 0.8f, 0.033f), (0.5f, 0.09f, 0.02f), (0.9f, 0.5f, 0.02f), (0.08f, 0.48f, 0.022f),
            };
            foreach (var d in dots)
            {
                float px = d.x * size, py = d.y * size, rad = d.s * size;
                if (!inside(px, py)) continue;
                r.FillCircle(px, py, rad, Pink, 0.35f);
            }
        }

        /// <summary>scale = how much of the icon width the group of flowers spans.</summary>
        private static void PaintFlowers(Raster r, int size, float scale)
        {
            float c = size * 0.5f;
            float group = size * scale;

            // Cream badge behind the rose, so the bordo rose stands out from the bordo background.
            r.SoftCircle(c, c + group * 0.03f, group * 0.62f, BordoDark, 0.45f);
            r.FillCircle(c, c, group * 0.47f, Pink);
            r.Fill(c - group, c - group, c + group, c + group,
                (x, y) => (x - c) * (x - c) + (y - c) * (y - c) < group * group * 0.43f * 0.43f,
                (x, y) => Raster.Color.Lerp(Raster.Color.Hex(0xFFFAF3), Cream, Math.Min(1f, Math.Max(0f, (y - c) / group + 0.5f))));

            const int art = 512;
            var rose = FlowerArt.Render(0, art);
            var heart = FlowerArt.Render(4, art);
            var blossom = FlowerArt.Render(2, art);

            r.Stamp(blossom, art, art, c - group * 0.3f, c - group * 0.26f, group * 0.34f, 1f, -16f);
            r.Stamp(rose, art, art, c - group * 0.02f, c + group * 0.02f, group * 0.78f);
            r.Stamp(heart, art, art, c + group * 0.3f, c + group * 0.27f, group * 0.36f, 1f, 14f);
        }

        private static bool InsideRoundedSquare(float x, float y, int size, float corner)
        {
            float qx = Math.Max(Math.Abs(x - size * 0.5f) - (size * 0.5f - corner), 0f);
            float qy = Math.Max(Math.Abs(y - size * 0.5f) - (size * 0.5f - corner), 0f);
            return qx * qx + qy * qy <= corner * corner;
        }
    }
}
