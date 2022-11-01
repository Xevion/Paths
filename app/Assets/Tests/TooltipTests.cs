using System.Collections;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests {
    /// <summary>
    /// Where the tooltip bubble actually lands. The cursor is given in screen pixels and the bubble is
    /// positioned in the scaled units its canvas lays out in, so the two have to be converted between
    /// rather than assumed equal. They are only equal at a scale of exactly 1, which no real window
    /// gives: this runner sits at the scale floor and a large window sits above 1.
    /// </summary>
    public class TooltipTests {
        private FakePointer _pointer;

        [SetUp]
        public void SetUp() {
            PointerInput.Reset();
            _pointer = new FakePointer();
            PointerInput.Source = _pointer;
        }

        [TearDown]
        public void TearDown() {
            UiScale.User = 1f;
            // DontDestroyOnLoad, so it would otherwise outlive this fixture carrying its last text
            if (TooltipLayer.Exists)
                Object.DestroyImmediate(TooltipLayer.Instance.gameObject);
            PointerInput.Reset();
        }

        private static RectTransform Bubble() => (RectTransform) TooltipLayer.Instance.transform.GetChild(0);

        private static float Scale() => TooltipLayer.Instance.GetComponent<Canvas>().scaleFactor;

        private static Vector2 Canvas() => ((RectTransform) TooltipLayer.Instance.transform).rect.size;

        [UnityTest]
        public IEnumerator TheBubbleSitsAtTheCursorWhateverTheInterfaceScale() {
            var at = new Vector2(Screen.width * 0.3f, Screen.height * 0.3f);
            _pointer.Position = at;
            TooltipLayer.Instance.Show("Grid width in cells");
            yield return null;

            // read as pixels the bubble lands right only at a scale of exactly 1, so a runner sitting
            // there would pass this with the conversion removed
            Assert.Greater(Mathf.Abs(Scale() - 1f), 0.01f,
                "the interface is at scale 1, so this proves nothing");

            Vector2 expected = at / Scale() + new Vector2(16f, 16f);
            Assert.AreEqual(expected.x, Bubble().anchoredPosition.x, 1f, "the bubble is off sideways");
            Assert.AreEqual(expected.y, Bubble().anchoredPosition.y, 1f, "the bubble is off vertically");
        }

        /// <summary>
        /// Near an edge it flips to the other side of the cursor and stays inside the window doing it.
        /// Run above a scale of 1, where the canvas is smaller than the screen it covers: that is the
        /// case a bubble kept inside the pixel count rather than the canvas overflows, and the one a
        /// maximised window puts the interface in.
        /// </summary>
        [UnityTest]
        public IEnumerator TheBubbleStaysInsideTheWindowNearTheEdge() {
            UiScale.User = 2f;
            _pointer.Position = new Vector2(Screen.width - 40f, Screen.height - 40f);
            TooltipLayer.Instance.Show("D* Lite - repair the plan as you edit instead of searching again");
            yield return null; // let UiScale push the new factor into the scaler
            TooltipLayer.Instance.Show("D* Lite - repair the plan as you edit instead of searching again");

            Assert.Greater(Scale(), 1.01f, "the interface is not scaled up, so this proves nothing");
            RectTransform bubble = Bubble();
            Vector2 bounds = Canvas();
            Assert.Greater(bubble.rect.width, bounds.x - Screen.width * 0.5f,
                "the bubble is too narrow to reach either edge, so this proves nothing");
            Assert.LessOrEqual(bubble.anchoredPosition.x + bubble.rect.width, bounds.x + 1f,
                "the bubble runs off the right of the window");
            Assert.LessOrEqual(bubble.anchoredPosition.y + bubble.rect.height, bounds.y + 1f,
                "the bubble runs off the top of the window");
            Assert.GreaterOrEqual(bubble.anchoredPosition.x, 0f, "the bubble flipped off the left instead");
            Assert.GreaterOrEqual(bubble.anchoredPosition.y, 0f, "the bubble flipped off the bottom instead");
        }

        /// <summary>
        /// A bubble is placed against its own width, so a longer tooltip shown where a shorter one was
        /// has to be measured before it is placed rather than a frame later.
        /// </summary>
        [UnityTest]
        public IEnumerator ALongerTooltipIsPlacedAgainstItsOwnWidth() {
            _pointer.Position = new Vector2(Screen.width - 2f, Screen.height * 0.5f);
            TooltipLayer.Instance.Show("Seed");
            yield return null;

            TooltipLayer.Instance.Show("Random seed - same seed regenerates the same maze");
            RectTransform bubble = Bubble();
            Assert.LessOrEqual(bubble.anchoredPosition.x + bubble.rect.width, Canvas().x + 1f,
                "the longer tooltip was placed where the shorter one fit");
            yield break;
        }
    }
}
