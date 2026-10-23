using System.IO;
using UnityEditor;
using UnityEngine;

namespace Flowers.Editor
{
    /// <summary>
    /// Builds every sprite the game uses and imports it with the right settings:
    /// - Art/Flowers: the five flowers, drawn by <see cref="FlowerArt"/>.
    /// - Art/UI: Kenney UI Pack (CC0) buttons, panel and star, turned into tintable grayscale (<see cref="TintableUi"/>).
    /// - Art/Shapes: simple white shapes (tiles, frame, mask, check, lock, dot, petal), tinted in the game.
    /// Safe to run again: files are regenerated in place, so references and .meta files stay.
    /// </summary>
    public static class GameArt
    {
        public const string FlowersFolder = "Assets/_Project/Art/Flowers";
        public const string UiFolder = "Assets/_Project/Art/UI";
        public const string ShapesFolder = "Assets/_Project/Art/Shapes";
        public const string KenneyFolder = "Assets/_Project/Art/ThirdParty/KenneyUI";

        public const string ButtonPath = UiFolder + "/Button.png";
        public const string PanelPath = UiFolder + "/Panel.png";
        public const string RoundButtonPath = UiFolder + "/RoundButton.png";
        public const string StarPath = UiFolder + "/Star.png";
        public const string CheckPath = ShapesFolder + "/Check.png";
        public const string CirclePath = ShapesFolder + "/Circle.png";
        public const string LockPath = ShapesFolder + "/Lock.png";
        public const string PetalPath = ShapesFolder + "/Petal.png";

        public const string BackgroundsFolder = "Assets/_Project/Art/Backgrounds";
        public const string GardenPath = BackgroundsFolder + "/Garden.png";
        public const string MapMeadowPath = BackgroundsFolder + "/MapMeadow.png";
        public const string MapSkyCapPath = BackgroundsFolder + "/MapSkyCap.png";
        public const string RibbonPath = UiFolder + "/Ribbon.png";
        public const string RaysPath = UiFolder + "/Rays.png";
        public const string MarkerPath = UiFolder + "/MapMarker.png";

        public const string ObstaclesFolder = "Assets/_Project/Art/Obstacles";

        private const int FlowerSize = 256;
        private const int TileSize = 128;
        private const int FrameSize = 128;
        private const int FrameBorder = 40;
        private const int MaskSize = 8;
        private const int IconSize = 128;

        /// <summary>UI sprites use 100 pixels per unit, matching the Canvas, so 9-slice borders keep their pixel size.</summary>
        private const int UiPixelsPerUnit = 100;

        // 9-slice borders of the Kenney "Double" (2x) buttons: 4px outline, ~16px round corners, ~12px depth at the bottom.
        private static readonly Vector4 KenneyBorder = new Vector4(20, 28, 20, 20); // left, bottom, right, top

        public struct Result
        {
            public Sprite[] Flowers;
            public Sprite Tile;
            public Sprite Frame;
            public Sprite Mask;
            public Sprite Petal;
            public Sprite Pot;
            public Sprite PotCracked;
            public Sprite Vine;
            public Sprite Soil;
        }

        public static Result GenerateAll()
        {
            EditorFolders.Ensure(FlowersFolder);
            EditorFolders.Ensure(UiFolder);
            EditorFolders.Ensure(ShapesFolder);

            var result = new Result { Flowers = new Sprite[FlowerArt.TypeCount] };
            for (int type = 0; type < FlowerArt.TypeCount; type++)
            {
                string path = $"{FlowersFolder}/Flower_{type}_{FlowerArt.Names[type]}.png";
                result.Flowers[type] = WriteSprite(path, FlowerArt.Render(type, FlowerSize), FlowerSize, FlowerSize, FlowerSize, Vector4.zero);
            }

            MakeTintable($"{KenneyFolder}/button_rectangle_depth_gradient.png", ButtonPath, KenneyBorder);
            MakeTintable($"{KenneyFolder}/button_square_depth_gradient.png", PanelPath, KenneyBorder);
            MakeTintable($"{KenneyFolder}/button_round_depth_gradient.png", RoundButtonPath, Vector4.zero);
            MakeTintable($"{KenneyFolder}/star.png", StarPath, Vector4.zero);

            result.Tile = WriteSquare($"{ShapesFolder}/Tile.png", PlaceholderShapes.RenderRoundedSquare(TileSize, 0.28f), TileSize, 0);
            result.Frame = WriteSquare($"{ShapesFolder}/Frame.png", PlaceholderShapes.RenderRoundedSquare(FrameSize, 0.6f), FrameSize, FrameBorder);
            result.Mask = WriteSquare($"{ShapesFolder}/Mask.png", PlaceholderShapes.RenderSolidSquare(MaskSize), MaskSize, 0);
            WriteSquare(CheckPath, PlaceholderShapes.RenderCheck(IconSize), IconSize, 0);
            WriteSquare(CirclePath, PlaceholderShapes.RenderCircle(IconSize), IconSize, 0);
            WriteSquare(LockPath, PlaceholderShapes.RenderLock(IconSize), IconSize, 0);
            result.Petal = WriteSquare(PetalPath, PlaceholderShapes.RenderPetal(64), 64, 0);

            // Backgrounds: drawn at roughly 2/3 of the 1080x1920 screen; soft art scales up fine.
            EditorFolders.Ensure(BackgroundsFolder);
            WriteSprite(GardenPath, BackgroundArt.Garden(720, 1280), 720, 1280, UiPixelsPerUnit, Vector4.zero);
            WriteSprite(MapMeadowPath, BackgroundArt.MapMeadow(720, 1080), 720, 1080, UiPixelsPerUnit, Vector4.zero);
            WriteSprite(MapSkyCapPath, BackgroundArt.MapSkyCap(720, 540), 720, 540, UiPixelsPerUnit, Vector4.zero);

            // Ribbon is 9-sliced horizontally only: the tails stay as drawn, the middle band stretches.
            WriteSprite(RibbonPath, BackgroundArt.Ribbon(640, 200), 640, 200, UiPixelsPerUnit, new Vector4(120, 0, 120, 0));
            WriteSprite(RaysPath, BackgroundArt.Rays(512), 512, 512, UiPixelsPerUnit, Vector4.zero);
            WriteSprite(MarkerPath, BackgroundArt.MapMarker(128), 128, 128, UiPixelsPerUnit, Vector4.zero);

            // Obstacles: 1 world unit wide, like the flowers.
            EditorFolders.Ensure(ObstaclesFolder);
            result.Pot = WriteSquare($"{ObstaclesFolder}/Pot.png", ObstacleArt.Pot(FlowerSize, false), FlowerSize, 0);
            result.PotCracked = WriteSquare($"{ObstaclesFolder}/PotCracked.png", ObstacleArt.Pot(FlowerSize, true), FlowerSize, 0);
            result.Vine = WriteSquare($"{ObstaclesFolder}/Vine.png", ObstacleArt.Vine(FlowerSize), FlowerSize, 0);
            result.Soil = WriteSquare($"{ObstaclesFolder}/SoilTile.png", ObstacleArt.SoilTile(TileSize), TileSize, 0);
            return result;
        }

        /// <summary>Reads a Kenney PNG from disk, converts it to tintable grayscale and saves it as a UI sprite.</summary>
        private static void MakeTintable(string sourcePath, string targetPath, Vector4 border)
        {
            if (!File.Exists(sourcePath))
            {
                Debug.LogError($"[Flowers] Missing Kenney source image: {sourcePath}");
                return;
            }

            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(sourcePath));
            var pixels = source.GetPixels32();
            var rgba = new byte[pixels.Length * 4];
            for (int i = 0; i < pixels.Length; i++)
            {
                rgba[i * 4] = pixels[i].r;
                rgba[i * 4 + 1] = pixels[i].g;
                rgba[i * 4 + 2] = pixels[i].b;
                rgba[i * 4 + 3] = pixels[i].a;
            }
            int width = source.width, height = source.height;
            Object.DestroyImmediate(source);

            WriteSprite(targetPath, TintableUi.ToTintable(rgba), width, height, UiPixelsPerUnit, border);
        }

        /// <summary>Square sprite that is exactly 1 world unit wide (pixels per unit = size).</summary>
        private static Sprite WriteSquare(string assetPath, byte[] rgba, int size, int border) =>
            WriteSprite(assetPath, rgba, size, size, size, new Vector4(border, border, border, border));

        private static Sprite WriteSprite(string assetPath, byte[] rgba, int width, int height, float pixelsPerUnit, Vector4 border)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixelData(rgba, 0);
            texture.Apply();
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = border;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            // Full rect is needed for 9-slicing; for the rest it is also fine and keeps edges clean.
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }

    public static class EditorFolders
    {
        /// <summary>Creates "Assets/a/b/c" one level at a time, through the AssetDatabase so .meta files are made.</summary>
        public static void Ensure(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            Ensure(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
