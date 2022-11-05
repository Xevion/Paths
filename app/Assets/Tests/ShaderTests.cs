using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Tests {
    /// <summary>
    /// That the shaders compile at all, which nothing else here can tell. A batch import parses the
    /// ShaderLab wrapper without compiling what is inside the CGPROGRAM, and every board assertion
    /// reads a texture rather than the screen, so one rendering nothing but magenta leaves the suite
    /// green. Shader.isSupported is no help either: it comes back true for a shader that failed.
    /// </summary>
    public class ShaderTests {
        // vendored with TextMesh Pro, so their state is not ours to assert or to fix
        private const string Vendored = "TextMesh Pro";

        /// <summary>
        /// Swept rather than named, so a shader added later is covered by having been added. The
        /// editor is asked because the error is recorded at import and there is no runtime signal.
        /// </summary>
        [Test]
        public void EveryShaderInTheProjectCompiles() {
            #if UNITY_EDITOR
            var swept = new List<string>();
            var broken = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Shader", new[] { "Assets" })) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains(Vendored))
                    continue;
                swept.Add(path);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null)
                    broken.Add(path + " (would not load)");
                else if (ShaderUtil.ShaderHasError(shader))
                    broken.Add(path);
            }

            // a sweep that found nothing passes whatever state the shaders are actually in
            Assert.IsNotEmpty(swept, "the sweep found no shaders, so it proves nothing");
            Assert.IsEmpty(broken,
                $"{broken.Count} of {swept.Count} shaders do not compile: {string.Join(", ", broken)}");
            #else
            Assert.Fail("a shader compile error is only visible through the editor");
            #endif
        }
    }
}
