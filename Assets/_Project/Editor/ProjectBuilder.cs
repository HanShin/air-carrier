using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AetherArk.Editor
{
    public static class ProjectBuilder
    {
        private const string MainScene = "Assets/Scenes/Main.unity";

        public static void BuildMac()
        {
            EnsureAlwaysIncludedShader("UI/Default");
            EnsureAlwaysIncludedShader("UI/Default Font");
            EnsureAlwaysIncludedShader("Sprites/Default");

            var outputPath = Path.GetFullPath("Builds/AetherArk.app");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? "Builds");

            var standalone = UnityEditor.Build.NamedBuildTarget.Standalone;
            var previousBackend = PlayerSettings.GetScriptingBackend(standalone);
            BuildReport report;

            try
            {
                PlayerSettings.SetScriptingBackend(standalone, ScriptingImplementation.Mono2x);
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { MainScene },
                    locationPathName = outputPath,
                    target = BuildTarget.StandaloneOSX,
                    options = BuildOptions.Development
                });
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(standalone, previousBackend);
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"macOS build failed: {report.summary.result} " +
                    $"({report.summary.totalErrors} errors, {report.summary.totalWarnings} warnings)");
            }

            Console.WriteLine(
                $"Aether Ark macOS build succeeded: {outputPath} " +
                $"({report.summary.totalSize} bytes)");
        }

        private static void EnsureAlwaysIncludedShader(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException($"Required shader not found: {shaderName}");

            var settingsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (settingsAssets.Length == 0) throw new InvalidOperationException("GraphicsSettings.asset is unavailable.");

            var serializedSettings = new SerializedObject(settingsAssets[0]);
            var shaders = serializedSettings.FindProperty("m_AlwaysIncludedShaders");
            if (shaders == null) throw new InvalidOperationException("Always Included Shaders setting is unavailable.");

            for (var i = 0; i < shaders.arraySize; i++)
            {
                if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
            }

            shaders.InsertArrayElementAtIndex(shaders.arraySize);
            shaders.GetArrayElementAtIndex(shaders.arraySize - 1).objectReferenceValue = shader;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }
    }
}
