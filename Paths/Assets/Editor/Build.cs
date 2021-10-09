using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Editor {
    /// <summary>
    /// Headless build entry points so the player can be built (and then run) from the
    /// Makefile without opening the editor every time. Invoked via -executeMethod Editor.Build.Linux.
    /// </summary>
    public static class Build {
        // Whatever scenes are ticked in the build settings (just Default for now).
        static string[] Scenes =>
            EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        public static void Linux() {
            // repo-root/Build/Linux/Paths -- dataPath is <project>/Assets, so up two.
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Build/Linux/Paths"));

            var options = new BuildPlayerOptions {
                scenes = Scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };

            BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
            if (summary.result == BuildResult.Succeeded)
                Debug.Log($"Build succeeded: {summary.totalSize} bytes -> {output}");
            else
                Debug.LogError($"Build failed ({summary.result})");

            // batchmode won't set a useful exit code on its own
            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
