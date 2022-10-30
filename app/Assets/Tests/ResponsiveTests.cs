using System.Collections;
using System.Linq;
using System.Reflection;
using Game.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests {
    /// <summary>
    /// How the interface behaves when the window is too small for it. The runner is 640x480, which is
    /// already past the scale floor, so every fixture here runs in a genuinely constrained window: the
    /// toolbar has to shed width, the dialog has to cap itself and scroll.
    /// </summary>
    public class ResponsiveTests {
        private AppShell _shell;

        [UnitySetUp]
        public IEnumerator SetUp() {
            PointerInput.Reset();
            yield return AppScene.Load();
            _shell = UnityEngine.Object.FindObjectOfType<AppShell>();
            for (int i = 0; i < 5; i++) yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown() {
            PointerInput.Reset();
            yield return AppScene.Unload();
        }

        private RectTransform Toolbar() => (RectTransform) _shell.transform.Find("Root/Toolbar");

        [Test]
        public void TheScaleStopsShrinkingAtTheFloor() {
            Assert.AreEqual(UiScale.Floor, UiScale.For(320f, 240f), 0.001f, "a tiny window kept shrinking");
            Assert.Greater(UiScale.For(1920f, 1200f), 1f, "a large window stopped growing, so the floor clamps both ends");
        }

        [UnityTest]
        public IEnumerator EveryCanvasSharesTheScale() {
            _shell.OpenSettings();
            for (int i = 0; i < 5; i++) yield return null;
            var dialog = UnityEngine.Object.FindObjectOfType<SettingsDialog>();

            foreach (CanvasScaler scaler in UnityEngine.Object.FindObjectsOfType<CanvasScaler>()) {
                if (scaler.GetComponent<UiScale>() == null) continue;
                Assert.AreEqual(UiScale.Current, scaler.scaleFactor, 0.001f,
                    scaler.name + " renders at its own scale, not the interface's");
            }
            Assert.IsNotNull(dialog.GetComponent<UiScale>(), "the dialog does not follow the interface scale");
        }

        [UnityTest]
        public IEnumerator TheToolbarStaysInsideTheWindow() {
            RectTransform bar = Toolbar();
            var group = bar.GetComponent<HorizontalLayoutGroup>();
            float budget = ((RectTransform) bar.parent).rect.width;
            Assert.LessOrEqual(bar.rect.width, budget + 0.5f,
                "the toolbar is wider than the space it was given, so its right-hand controls are off screen");

            RectTransform last = bar.Cast<RectTransform>()
                .Where(c => c.gameObject.activeSelf && !Ignored(c))
                .OrderBy(c => c.anchoredPosition.x).Last();
            float right = last.anchoredPosition.x + last.rect.width * 0.5f + group.padding.right;
            Assert.LessOrEqual(right, bar.rect.width + 0.5f, "the last control hangs off the toolbar");
            yield break;
        }

        [UnityTest]
        public IEnumerator TheToolbarShedsWidthEvenlyBeforeDroppingAnything() {
            var steps = Toolbar().GetComponentsInChildren<AdaptiveSteps>(true).ToArray();
            Assert.IsNotEmpty(steps, "nothing on the toolbar declared a smaller form");
            Assert.IsTrue(steps.Any(s => s.Level > 0), "the toolbar never stepped down in a 640x480 window");
            // a control only takes a second step once everything else has taken a first
            int deepest = steps.Max(s => s.Level);
            foreach (AdaptiveSteps s in steps)
                Assert.GreaterOrEqual(s.Level, Mathf.Min(deepest - 1, s.Levels - 1),
                    s.name + " sat at full size while another control was stripped further");
            yield break;
        }

        [UnityTest]
        public IEnumerator TheScrubberStaysGrabbable() {
            RectTransform scrub = Toolbar().Cast<RectTransform>().First(c => c.name == "Slider");
            Assert.GreaterOrEqual(scrub.rect.width, 60f, "the scrubber was squeezed to nothing");
            yield break;
        }

        [UnityTest]
        public IEnumerator AButtonNeverAdvertisesLessWidthThanItDraws() {
            // the check is about the full form: at 640x480 Generate is already icon-only, and a lone
            // glyph can't collide with anything
            foreach (AdaptiveSteps steps in _shell.GetComponentsInChildren<AdaptiveSteps>(true))
                steps.SetLevel(0);
            LayoutRebuilder.ForceRebuildLayoutImmediate(Toolbar());
            yield return null;

            int inspected = 0;
            foreach (Button button in _shell.GetComponentsInChildren<Button>(true)) {
                var element = button.GetComponent<LayoutElement>();
                var group = button.GetComponent<HorizontalLayoutGroup>();
                TextMeshProUGUI[] parts = button.GetComponentsInChildren<TextMeshProUGUI>(false);
                // a lone glyph has nothing to collide with, however narrow the button gets
                if (element == null || group == null || parts.Length < 2) continue;

                float content = group.padding.left + group.padding.right + (parts.Length - 1) * group.spacing;
                foreach (TextMeshProUGUI part in parts) content += UIKit.Measure(part);
                Assert.GreaterOrEqual(element.minWidth, content - 0.5f,
                    button.name + " can be squeezed under its own content, which stacks its label on its icon");
                inspected++;
            }
            Assert.Greater(inspected, 0, "no icon-and-label button was on screen, so this proves nothing");
        }

        [UnityTest]
        public IEnumerator TheDialogFitsTheScreenAndScrollsWhatDoesNot() {
            _shell.OpenSettings();
            for (int i = 0; i < 5; i++) yield return null;
            var dialog = UnityEngine.Object.FindObjectOfType<SettingsDialog>();
            // the shortcuts tab is the tall one - the general tab fits at any size and proves nothing
            dialog.GetType().GetMethod("Select", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(dialog, new object[] { 1 });
            for (int i = 0; i < 5; i++) yield return null;

            var window = (RectTransform) dialog.transform.Find("Scrim/Panel");
            Assert.LessOrEqual(window.rect.height, Screen.height / UiScale.Current + 0.5f,
                "the dialog is taller than the window it opened in");
            Assert.LessOrEqual(window.rect.width, Screen.width / UiScale.Current + 0.5f,
                "the dialog is wider than the window it opened in");

            var viewport = (RectTransform) window.GetComponentsInChildren<RectMask2D>(true).First().transform;
            var body = (RectTransform) viewport.Find("Body");
            Assert.Greater(body.rect.height, viewport.rect.height,
                "the tallest tab fit the viewport, so this proves nothing about scrolling");
            Assert.IsNotNull(viewport.GetComponent<SidebarScroll>(), "the clipped body has no way to scroll");
        }

        private static bool Ignored(RectTransform rt) {
            var element = rt.GetComponent<LayoutElement>();
            return element != null && element.ignoreLayout;
        }
    }
}
