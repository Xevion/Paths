using System;
using UnityEngine;

namespace Game.UI {
    /// <summary>
    /// Spawns the UI showcases on demand: F2 toggles the settings modal, F3 the always-on desktop layout
    /// (or pass -showcase / -desktop to open one at startup). Auto-installed at load, no scene wiring.
    /// </summary>
    public class ShowcaseLauncher : MonoBehaviour {
        private GameObject _modal;
        private GameObject _desktop;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() {
            var go = new GameObject("ShowcaseLauncher");
            DontDestroyOnLoad(go);
            var launcher = go.AddComponent<ShowcaseLauncher>();
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-showcase") >= 0)
                launcher.OpenModal();
            if (Array.IndexOf(args, "-desktop") >= 0)
                launcher.OpenDesktop();
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.F2))
                Toggle(ref _modal, OpenModal);
            if (Input.GetKeyDown(KeyCode.F3))
                Toggle(ref _desktop, OpenDesktop);
        }

        private void Toggle(ref GameObject instance, System.Action open) {
            if (instance != null)
                Destroy(instance);
            else
                open();
        }

        private void OpenModal() {
            if (_modal != null) return;
            _modal = new GameObject("UI Showcase");
            _modal.AddComponent<UIShowcase>();
        }

        private void OpenDesktop() {
            if (_desktop != null) return;
            _desktop = new GameObject("Desktop Showcase");
            _desktop.AddComponent<DesktopShowcase>();
        }
    }
}
