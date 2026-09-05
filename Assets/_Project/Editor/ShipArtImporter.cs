using UnityEditor;
using UnityEngine;

namespace AetherArk.Editor
{
    /// <summary>Keeps generated ship cutouts, UI icons and campaign backgrounds ready for runtime loading.</summary>
    public sealed class ShipArtImporter : AssetPostprocessor
    {
        private const string ShipArtFolder = "Assets/_Project/Resources/Art/Ships/";
        private const string IconArtFolder = "Assets/_Project/Resources/Art/Icons/";
        private const string BackgroundArtFolder = "Assets/_Project/Resources/Art/Backgrounds/";

        public override uint GetVersion() => 4;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ShipArtFolder, System.StringComparison.Ordinal)
                && !assetPath.StartsWith(IconArtFolder, System.StringComparison.Ordinal)
                && !assetPath.StartsWith(BackgroundArtFolder, System.StringComparison.Ordinal)) return;

            var importer = (TextureImporter)assetImporter;
            var isBackground = assetPath.StartsWith(BackgroundArtFolder, System.StringComparison.Ordinal);
            importer.textureType = isBackground ? TextureImporterType.Default : TextureImporterType.Sprite;
            if (!isBackground) importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = !isBackground;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            // Keep the source aspect ratio and the measured cutaway UV bounds intact.
            importer.npotScale = TextureImporterNPOTScale.None;
            if (!isBackground)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
            }
            importer.maxTextureSize = assetPath.StartsWith(IconArtFolder, System.StringComparison.Ordinal) ? 256 : 2048;
        }
    }
}
