using System.IO;
using UnityEditor;
using UnityEngine;

namespace Flowers.Editor
{
    /// <summary>Writes the placeholder sprites as PNG files and imports them as sprites.</summary>
    public static class PlaceholderArt
    {
        public const string Folder = "Assets/_Project/Art/Placeholder";

        private const int FlowerSize = 256;
        private const int TileSize = 128;
        private const int FrameSize = 128;
        private const int FrameBorder = 40;
        private const int MaskSize = 8;
        private const int IconSize = 128;

        public struct Result
        {
            public Sprite[] Flowers;
            public Sprite Tile;
            public Sprite Frame;
            public Sprite Mask;
            public Sprite Star;
            public Sprite Check;
        }

        public static Result GenerateAll()
        {
            EditorFolders.Ensure(Folder);

            var result = new Result { Flowers = new Sprite[PlaceholderShapes.FlowerTypeCount] };
            for (int type = 0; type < PlaceholderShapes.FlowerTypeCount; type++)
            {
                string path = $"{Folder}/Flower_{type}_{PlaceholderShapes.FlowerNames[type]}.png";
                result.Flowers[type] = WriteSprite(path, PlaceholderShapes.RenderFlower(type, FlowerSize), FlowerSize, 0);
            }

            result.Tile = WriteSprite($"{Folder}/Tile.png", PlaceholderShapes.RenderRoundedSquare(TileSize, 0.28f), TileSize, 0);
            result.Frame = WriteSprite($"{Folder}/Frame.png", PlaceholderShapes.RenderRoundedSquare(FrameSize, 0.6f), FrameSize, FrameBorder);
            result.Mask = WriteSprite($"{Folder}/Mask.png", PlaceholderShapes.RenderSolidSquare(MaskSize), MaskSize, 0);
            result.Star = WriteSprite($"{Folder}/Star.png", PlaceholderShapes.RenderStar(IconSize), IconSize, 0);
            result.Check = WriteSprite($"{Folder}/Check.png", PlaceholderShapes.RenderCheck(IconSize), IconSize, 0);
            return result;
        }

        /// <summary>Saves raw RGBA pixels as a PNG and imports it as a sprite that is exactly 1 world unit wide.</summary>
        private static Sprite WriteSprite(string assetPath, byte[] rgba, int size, int border)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixelData(rgba, 0);
            texture.Apply();
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = new Vector4(border, border, border, border);

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
