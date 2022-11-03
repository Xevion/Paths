using System.Collections;
using System.Linq;
using Game.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests {
    /// <summary>
    /// The app's keyboard shortcuts against whatever owns the keyboard. A dialog takes every key, and a
    /// focused control takes the ones that drive it, so neither can be typed straight through into the
    /// board - pressing Space on a tab used to change the tab and toggle the search at the same time.
    /// </summary>
    public class ShortcutTests {
        private UIController _ui;
        private AppShell _shell;
        private FakeKeys _keys;

        [UnitySetUp]
        public IEnumerator SetUp() {
            PointerInput.Reset();
            _keys = new FakeKeys();
            KeyInput.Source = _keys;
            yield return AppScene.Load();
            _ui = UnityEngine.Object.FindObjectOfType<UIController>();
            _shell = UnityEngine.Object.FindObjectOfType<AppShell>();
            Deselect();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown() {
            KeyInput.Reset();
            PointerInput.Reset();
            yield return AppScene.Unload();
        }

        /// <summary>Hold a key down for one frame, the way a real press reads.</summary>
        private IEnumerator Press(KeyCode key) {
            _keys.Down = key;
            yield return null;
            _keys.Down = KeyCode.None;
            yield return null;
        }

        private static void Deselect() {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void Select(Selectable control) {
            EventSystem.current.SetSelectedGameObject(control.gameObject);
        }

        /// <summary>Any focusable control in the toolbar, to park the selection on.</summary>
        private Selectable AControl() {
            return _shell.GetComponentsInChildren<Button>(true).First(KeyboardFocus.IsFocusable);
        }

        [UnityTest]
        public IEnumerator SpaceTogglesTheSearch() {
            AnimationState before = _ui.State;
            yield return Press(KeyCode.Space);
            Assert.AreNotEqual(before, _ui.State, "Space did not reach the app at all");
        }

        [UnityTest]
        public IEnumerator BrushKeysArmTheBrush() {
            yield return Press(KeyCode.Alpha3);
            Assert.AreEqual(BrushKind.Terrain, _ui._brush);
            Assert.AreEqual(Paths.Core.Search.Terrain.Mud, _ui._tier);
        }

        [UnityTest]
        public IEnumerator SpaceOnAFocusedControlStaysWithTheControl() {
            Select(AControl());
            yield return null;
            AnimationState before = _ui.State;
            yield return Press(KeyCode.Space);
            Assert.AreEqual(before, _ui.State,
                "Space reached the app while a control had focus, so the control's own submit toggles it twice");
        }

        [UnityTest]
        public IEnumerator AKeyAControlDoesNotWantStillReachesTheApp() {
            Select(AControl());
            yield return null;
            yield return Press(KeyCode.Alpha2);
            Assert.AreEqual(BrushKind.Terrain, _ui._brush, "a focused control swallowed a key it does not use");
            Assert.AreEqual(Paths.Core.Search.Terrain.Road, _ui._tier);
        }

        /// <summary>
        /// The one shortcut the reference could never name, because there was no way to reach it
        /// from the keyboard. It has to land on the list rather than the General tab, which is the
        /// whole point of pressing it.
        /// </summary>
        [UnityTest]
        public IEnumerator TheQuestionMarkOpensTheShortcutList() {
            yield return Press(KeyCode.Slash);
            yield return null;
            yield return null;

            var dialog = UnityEngine.Object.FindObjectOfType<SettingsDialog>();
            Assert.IsNotNull(dialog, "the shortcut key did not open the dialog");

            TextMeshProUGUI shown = dialog.GetComponentsInChildren<TextMeshProUGUI>(false)
                .FirstOrDefault(t => t.text == "Play or pause the search");
            Assert.IsNotNull(shown, "the dialog opened on some other tab than the shortcut list");
        }

        /// <summary>The list documents the key that opens it, which is what it could not before.</summary>
        [UnityTest]
        public IEnumerator TheShortcutListNamesTheKeyThatOpensIt() {
            _shell.OpenSettings(1);
            yield return null;
            yield return null;

            var dialog = UnityEngine.Object.FindObjectOfType<SettingsDialog>();
            Assert.IsNotNull(dialog, "the dialog never opened");
            bool named = dialog.GetComponentsInChildren<TextMeshProUGUI>(false).Any(t => t.text == "?");
            Assert.IsTrue(named, "the shortcut list does not mention the key that opens it");
        }

        [UnityTest]
        public IEnumerator AnOpenDialogTakesEveryKey() {
            _shell.OpenSettings();
            yield return null;
            yield return null;
            Assert.IsNotNull(UnityEngine.Object.FindObjectOfType<SettingsDialog>(), "the dialog never opened");

            AnimationState state = _ui.State;
            BrushKind brush = _ui._brush;
            int width = _ui._solver.Grid.Width;

            yield return Press(KeyCode.Space);
            yield return Press(KeyCode.Alpha4);
            yield return Press(KeyCode.Equals);

            Assert.AreEqual(state, _ui.State, "Space reached the app through an open dialog");
            Assert.AreEqual(brush, _ui._brush, "a brush key reached the app through an open dialog");
            Assert.AreEqual(width, _ui._solver.Grid.Width, "a resize key reached the app through an open dialog");
        }

        [UnityTest]
        public IEnumerator ClosingTheDialogHandsTheKeyboardBack() {
            _shell.OpenSettings();
            yield return null;
            yield return Press(KeyCode.Escape);
            // the close animates out before destroying, so give it a moment
            for (int i = 0; i < 40 && UnityEngine.Object.FindObjectOfType<SettingsDialog>() != null; i++)
                yield return null;
            Assert.IsNull(UnityEngine.Object.FindObjectOfType<SettingsDialog>(), "Escape did not close the dialog");

            AnimationState before = _ui.State;
            yield return Press(KeyCode.Space);
            Assert.AreNotEqual(before, _ui.State, "the app never got the keyboard back");
        }
    }

    internal class FakeKeys : IKeySource {
        public KeyCode Down = KeyCode.None;

        public bool Held(KeyCode key) => key == Down;
        public bool Pressed(KeyCode key) => key == Down;
    }
}
