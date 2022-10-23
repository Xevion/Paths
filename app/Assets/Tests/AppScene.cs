using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests {
    /// <summary>
    /// Putting the real app up and taking it down again for a fixture.
    ///
    /// Taking it down matters more than it looks. The unload's destroys are deferred, so anything
    /// still standing a frame later is inherited by whatever runs next: an event system, which
    /// PointerBaseInput adopts instead of building one that reads the scripted pointer, or the
    /// sidebar, which sits on the right of the screen and makes a press out there look like it
    /// landed on the interface.
    /// </summary>
    internal static class AppScene {
        private const string Name = "Default";

        public static IEnumerator Load() {
            yield return SceneManager.LoadSceneAsync(Name, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(Name));
            yield return null;
            yield return null;
        }

        public static IEnumerator Unload() {
            Scene scene = SceneManager.GetSceneByName(Name);
            if (scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);

            Clear();
            // a second pass: the first frame is when the unload's deferred destroys actually land
            yield return null;
            Clear();
        }

        /// <summary>
        /// What the app has left standing, named, or empty when the screen is clear. A fixture that
        /// builds its own screen asserts on this, and the names are what makes a leak diagnosable
        /// rather than a mystery about presses that go nowhere.
        /// </summary>
        public static string LeftStanding() {
            var names = new System.Collections.Generic.List<string>();
            foreach (EventSystem system in Object.FindObjectsOfType<EventSystem>())
                names.Add($"EventSystem '{system.gameObject.name}'");
            // only a canvas that raycasts can swallow a press. the frozen cursor is a DontDestroyOnLoad
            // singleton that deliberately outlives a scene, and it never raycasts, so it is not a leak
            foreach (GraphicRaycaster raycaster in Object.FindObjectsOfType<GraphicRaycaster>())
                names.Add($"Canvas '{raycaster.gameObject.name}'");
            return string.Join(", ", names);
        }

        private static void Clear() {
            foreach (EventSystem system in Object.FindObjectsOfType<EventSystem>())
                Object.DestroyImmediate(system.gameObject);
            foreach (GraphicRaycaster raycaster in Object.FindObjectsOfType<GraphicRaycaster>())
                Object.DestroyImmediate(raycaster.gameObject);
        }
    }
}
