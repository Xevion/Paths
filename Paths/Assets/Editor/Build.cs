using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Editor {
    /// <summary>
    /// Headless build entry points so the player can be built (and then run) from the
    /// Makefile without opening the editor every time. Invoked via -executeMethod Editor.Build.*.
    /// </summary>
    public static class Build {
        // Whatever scenes are ticked in the build settings (just Default for now).
        static string[] Scenes =>
            EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        static string OutPath(string sub) =>
            // dataPath is <project>/Assets, so up two to the repo root.
            Path.GetFullPath(Path.Combine(Application.dataPath, "../../Build", sub));

        public static void Linux() {
            // launch windowed by default - fullscreen-on-launch is annoying for a demo you just
            // want to poke at, and the -screen-fullscreen 0 CLI flag never stuck reliably.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.resizableWindow = true;

            Run(new BuildPlayerOptions {
                scenes = Scenes,
                locationPathName = OutPath("Linux/Paths"),
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            });
        }

        public static void WebGL() {
            // WebGL2 only - the grid shader's dynamic colour-array index doesn't compile under
            // GLES2, and every browser worth targeting in 2021 does WebGL2 anyway.
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            // ships a JS decompressor so it loads off a plain static server (python http.server,
            // GitHub Pages) without the host having to set Content-Encoding headers.
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.runInBackground = true;

            Run(new BuildPlayerOptions {
                scenes = Scenes,
                locationPathName = OutPath("WebGL"),
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
        }

        static void Run(BuildPlayerOptions options) {
            BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
            if (summary.result == BuildResult.Succeeded)
                Debug.Log($"Build succeeded: {summary.totalSize} bytes -> {options.locationPathName}");
            else
                Debug.LogError($"Build failed ({summary.result})");

            // batchmode won't set a useful exit code on its own
            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
