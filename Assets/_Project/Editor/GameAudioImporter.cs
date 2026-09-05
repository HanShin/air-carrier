using System;
using UnityEditor;
using UnityEngine;

namespace AetherArk.Editor
{
    public sealed class GameAudioImporter : AssetPostprocessor
    {
        public override uint GetVersion() => 1;

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/_Project/Resources/Audio/", StringComparison.Ordinal)) return;
            var importer = (AudioImporter)assetImporter;
            var music = assetPath.Contains("/Music/");
            importer.forceToMono = false;
            importer.loadInBackground = false;
            var settings = importer.defaultSampleSettings;
            // Compressed resident loops avoid disk streaming stalls at transitions; short cues are PCM.
            settings.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            settings.quality = 0.8f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
        }
    }
}
