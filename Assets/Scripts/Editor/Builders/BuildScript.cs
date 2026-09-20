using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor.Builders
{
    /// <summary>
    /// Headless player builds. Each entry point first re-runs the idempotent builder chain,
    /// then builds the enabled EditorBuildSettings scenes (00_Boot, 01_MainMenu, 02_Gameplay —
    /// 99_Dev_BoardSandbox is never included).
    ///   unity run D:\unity\practice2 -- -executeMethod Game.Editor.Builders.BuildScript.RunAndroid
    /// </summary>
    public static class BuildScript
    {
        public static void RunAllBuilders()
        {
            Debug.Log("[BuildScript] Running full builder chain...");
            ProjectSetup.Run();
            SpriteForge.Run();
            PrefabBuilder.Run();
            LevelAssetBuilder.Run();
            SceneBuilder.Run();
            Debug.Log("[BuildScript] Full builder chain completed.");
        }

        [MenuItem("Tools/Block Breaker/Build Standalone Windows", false, 200)]
        public static void Run()
        {
            Build(BuildTarget.StandaloneWindows64, "Builds/StandaloneWindows64/BlockBreaker.exe");
        }

        [MenuItem("Tools/Block Breaker/Build Android", false, 201)]
        public static void RunAndroid()
        {
            Build(BuildTarget.Android, "Builds/Android/BlockBreaker.apk");
        }

        private static void Build(BuildTarget target, string buildPath)
        {
            Debug.Log($"[BuildScript] Starting headless {target} build...");

            // Ensure full builder chain has run
            RunAllBuilders();

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildScript] No scenes found in EditorBuildSettings. Cannot build.");
                ExitIfBatch(1);
                return;
            }

            string buildDir = Path.GetDirectoryName(buildPath);
            if (!string.IsNullOrEmpty(buildDir) && !Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = target,
                options = BuildOptions.None
            };

            Debug.Log($"[BuildScript] Building {scenes.Length} scenes to {buildPath}...");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] Build succeeded: {summary.totalSize} bytes in {summary.totalTime.TotalSeconds:F1}s.");
                ExitIfBatch(0);
            }
            else
            {
                Debug.LogError($"[BuildScript] Build failed with result: {summary.result}. Total errors: {summary.totalErrors}");
                ExitIfBatch(1);
            }
        }

        private static void ExitIfBatch(int code)
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }
    }
}
