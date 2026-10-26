using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace Flowers.Editor
{
    /// <summary>
    /// Draws the app icon (<see cref="AppIconArt"/>) and sets it in Player Settings for Android:
    /// adaptive (background + foreground layers, Android 8+), round and legacy, plus the default icon.
    /// Also run by Flowers → Project Setup. Safe to run again.
    /// </summary>
    public static class AppIconMenu
    {
        public const string Folder = "Assets/_Project/Art/AppIcon";
        private const int Size = 432; // xxxhdpi adaptive layer size (108dp x 4); Unity scales it down for the others

        [MenuItem("Flowers/Make App Icon")]
        public static void MakeAppIcon()
        {
            EditorFolders.Ensure(Folder);
            var background = Write("IconBackground", AppIconArt.Background(Size));
            var foreground = Write("IconForeground", AppIconArt.Foreground(Size));
            var legacy = Write("IconLegacy", AppIconArt.Legacy(Size, false));
            var round = Write("IconRound", AppIconArt.Legacy(Size, true));
            if (background == null || foreground == null || legacy == null || round == null)
            {
                Debug.LogError($"[Flowers] Could not load the app icon textures from {Folder}.");
                return;
            }

            var android = NamedBuildTarget.Android;
            SetAll(android, AndroidPlatformIconKind.Adaptive, background, foreground);
            SetAll(android, AndroidPlatformIconKind.Round, round);
            SetAll(android, AndroidPlatformIconKind.Legacy, legacy);
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { legacy }, IconKind.Application);

            AssetDatabase.SaveAssets();
            Debug.Log("[Flowers] App icon set (Android adaptive, round, legacy and default). Build a new APK to see it on the phone.");
        }

        /// <summary>Puts the same texture(s) into every density slot. Adaptive icons take (background, foreground).</summary>
        private static void SetAll(NamedBuildTarget target, PlatformIconKind kind, params Texture2D[] layers)
        {
            var icons = PlayerSettings.GetPlatformIcons(target, kind);
            foreach (var icon in icons)
                icon.SetTextures(layers);
            PlayerSettings.SetPlatformIcons(target, kind, icons);
        }

        private static Texture2D Write(string name, byte[] bottomUpRgba)
        {
            string path = $"{Folder}/{name}.png";
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.SetPixelData(bottomUpRgba, 0);
            texture.Apply();
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
