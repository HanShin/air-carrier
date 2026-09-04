using UnityEditor;
using UnityEngine;

namespace AetherArk.Editor
{
    /// <summary>Keeps generated ship cutouts and UI icons ready for Resources.Load&lt;Sprite&gt;.</summary>
    public sealed class ShipArtImporter : AssetPostprocessor
    {
        private const string ShipArtFolder = "Assets/_Project/Resources/Art/Ships/";
        private const string IconArtFolder = "Assets/_Project/Resources/Art/Icons/";

        public override uint GetVersion() => 2;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ShipArtFolder, System.StringComparison.Ordinal)
                && !assetPath.StartsWith(IconArtFolder, System.StringComparison.Ordinal)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = assetPath.StartsWith(IconArtFolder, System.StringComparison.Ordinal) ? 256 : 2048;
        }
    }
}
