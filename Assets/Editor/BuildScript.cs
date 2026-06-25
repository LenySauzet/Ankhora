using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Ankhora.Editor
{
    /// <summary>
    /// Headless Quest 3 APK build entry point, invoked by the <c>/build-android</c> command:
    /// <code>
    /// Unity -batchmode -quit -nographics -projectPath . -buildTarget Android \
    ///   -executeMethod Ankhora.Editor.BuildScript.BuildQuestApk \
    ///   -outputPath Build/Android/Ankhora-&lt;mode&gt;-&lt;sha&gt;.apk [-development]
    /// </code>
    /// Forces the Quest target invariants (Android / IL2CPP / ARM64) defensively, then builds
    /// the enabled scenes from Build Settings. The Meta SDK ↔ Gradle 9 namespace clash is
    /// handled separately by <see cref="MetaAarNamespacePatcher"/> (an IPreprocessBuildWithReport).
    /// </summary>
    public static class BuildScript
    {
        private const string DefaultOutput = "Build/Android/Ankhora.apk";

        public static void BuildQuestApk()
        {
            string outputPath = GetArg("-outputPath") ?? DefaultOutput;
            bool development = HasFlag("-development");

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
                Fail("No enabled scenes in Build Settings — add at least one (MainVrScene).");

            // Quest target invariants (defensive — normally already set in ProjectSettings).
            EnsureAndroidTarget();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)));

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = development
                    ? BuildOptions.Development | BuildOptions.AllowDebugging
                    : BuildOptions.None,
            };

            Debug.Log($"[BuildScript] Building {(development ? "dev" : "release")} APK -> {outputPath} " +
                      $"({scenes.Length} scene(s): {string.Join(", ", scenes)})");

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
                Fail($"Build {summary.result}: {summary.totalErrors} error(s).");

            Debug.Log($"[BuildScript] Build succeeded: {summary.outputPath} " +
                      $"({summary.totalSize / (1024 * 1024)} MB, {summary.totalWarnings} warning(s), " +
                      $"{summary.totalTime.TotalSeconds:F0}s).");
        }

        private static void EnsureAndroidTarget()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("[BuildScript] Switching active build target to Android...");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                    Fail("Failed to switch active build target to Android.");
            }
        }

        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                    return args[i + 1];
            }
            return null;
        }

        private static bool HasFlag(string name) => Environment.GetCommandLineArgs().Contains(name);

        private static void Fail(string message)
        {
            Debug.LogError($"[BuildScript] {message}");
            // Non-zero exit so CI / the /build-android command sees the failure in batchmode.
            EditorApplication.Exit(1);
        }
    }
}
