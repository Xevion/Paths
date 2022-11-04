using System.Collections;
using System.Collections.Generic;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests {
    /// <summary>
    /// Which parts of a form row answer a hover, read off a real raycast rather than off the
    /// hierarchy. A tooltip lives on the row and is raised by anything under it, so what decides the
    /// hover zone is where the raycast surfaces are, which no assertion about components can see.
    /// </summary>
    public class FieldHoverTests {
        private const float PanelWidth = 320f;
        private const float LabelWidth = 150f;

        private GameObject _canvas;
        private RectTransform _panel;
        private RectTransform _row;
        private RectTransform _sliderRow;
        private int _hitCount; // how much the last probe hit, set by Hovered

        [UnitySetUp]
        public IEnumerator SetUp() {
            Assert.IsEmpty(AppScene.LeftStanding(), "a previous fixture left something on screen");
            PointerBaseInput.EnsureEventSystem();

            _canvas = new GameObject("Canvas", typeof(RectTransform));
            Canvas canvas = _canvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.AddComponent<GraphicRaycaster>();

            // a raycast surface behind the row, so a probe that hits nothing on the row still hits
            // something: an empty hit list would pass a cold assertion by missing the panel entirely
            _panel = New("Panel", _canvas.transform);
            _panel.anchorMin = _panel.anchorMax = _panel.pivot = new Vector2(0f, 1f);
            _panel.sizeDelta = new Vector2(PanelWidth, 200f);
            _panel.anchoredPosition = Vector2.zero;
            _panel.gameObject.AddComponent<Image>().raycastTarget = true;
            var v = _panel.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            // "Seed" is short, so its slot is several times its text: the gap the affordance does not
            // mark but the row surface used to accept
            _row = UIKit.Field(_panel, Fonts.Regular, "Seed", tooltip: "Random seed", labelWidth: LabelWidth);
            UIKit.Switch(_row, false, on => { });

            // the handled slider, whose visible parts cover least of the control it fills
            _sliderRow = UIKit.Field(_panel, Fonts.Regular, "Weight", fill: true, tooltip: "Heuristic weight",
                labelWidth: LabelWidth);
            UIKit.Slider(_sliderRow, 0f, 1f, 0.5f, value => { });
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);

            // a graphic is invisible to a raycast until the canvas has batched it and given it a
            // depth, which is a rendered frame away however finished the layout is
            yield return null;
        }

        [TearDown]
        public void TearDown() {
            // immediate, not deferred: the next fixture runs before a frame passes, so a Destroy here
            // leaves the screen still standing when it looks
            if (_canvas != null) Object.DestroyImmediate(_canvas);
            if (EventSystem.current != null) Object.DestroyImmediate(EventSystem.current.gameObject);
        }

        private static RectTransform New(string name, Transform parent) {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform) go.transform;
        }

        private RectTransform Child(Transform parent, string name) {
            Transform found = parent.Find(name);
            Assert.IsNotNull(found, $"no '{name}' under '{parent.name}'");
            return (RectTransform) found;
        }

        private RectTransform Caption => Child(_row, "Label");
        private RectTransform Zone => Child(Caption, "Hover");
        private RectTransform Spacer => Child(_row, "Spacer");
        private RectTransform Widget => Child(_row, "Switch");

        // a point inside a rect, given as a 0..1 position across it
        private static Vector2 PointOn(RectTransform rt, float x, float y = 0.5f) {
            Rect r = rt.rect;
            Vector3 world = rt.TransformPoint(new Vector3(r.xMin + r.width * x, r.yMin + r.height * y, 0f));
            return RectTransformUtility.WorldToScreenPoint(null, world);
        }

        /// <summary>
        /// The tooltip a hover at this point would raise, which is the one on the topmost thing hit or
        /// on any of its ancestors, exactly as the event system dispatches enter and exit.
        /// </summary>
        private Tooltip Hovered(Vector2 at, out string seam) {
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = at }, hits);
            var under = new List<string>();
            foreach (RaycastResult hit in hits)
                under.Add(hit.gameObject.name);
            _hitCount = hits.Count;
            seam = $" (at {at}, hit [{string.Join(", ", under)}])";
            return hits.Count == 0 ? null : hits[0].gameObject.GetComponentInParent<Tooltip>();
        }

        private void AssertHot(Vector2 at, string what) {
            Tooltip tip = Hovered(at, out string seam);
            Assert.IsNotNull(tip, $"hovering {what} raises nothing" + seam);
            Assert.AreEqual("Random seed", tip.text, $"hovering {what} raises the wrong thing" + seam);
        }

        private void AssertCold(Vector2 at, string what) {
            Tooltip tip = Hovered(at, out string seam);
            // the panel is behind the whole row, so a probe that hit nothing missed the screen and
            // would read as cold wherever it was aimed
            Assert.Greater(_hitCount, 0, $"the probe for {what} landed off the panel" + seam);
            Assert.IsNull(tip, $"hovering {what} raises a tooltip" + seam);
        }

        [Test]
        public void TheLabelAndTheControlAnswerAHover() {
            AssertHot(PointOn(Zone, 0.5f), "the label");
            AssertHot(PointOn(Widget, 0.5f), "the control");
        }

        /// <summary>
        /// The two stretches the row covers that nothing marks: the gap pushing the control right, and
        /// the rest of the label's fixed-width slot past the end of its text.
        /// </summary>
        [Test]
        public void TheEmptyStretchesOfTheRowDoNot() {
            Assert.Greater(Spacer.rect.width, 8f, "there is no gap in this row, so this proves nothing");
            Assert.Less(Zone.rect.width, Caption.rect.width - 8f,
                "the hover zone fills the label slot, so the slot probe proves nothing");

            AssertCold(PointOn(Spacer, 0.5f), "the gap between the label and the control");
            // between the end of the text and the end of the slot
            float past = (Zone.rect.width + Caption.rect.width) * 0.5f / Caption.rect.width;
            AssertCold(PointOn(Caption, past), "the empty end of the label slot");
        }

        /// <summary>
        /// The handled slider draws a 6px groove in a 24px control, which is a thin thing to find with
        /// a cursor. The whole control answers instead, both to a hover and to a grab. A field row is
        /// the harness here; the control this one is really about is the transport scrubber.
        /// </summary>
        [Test]
        public void ASliderAnswersOverItsWholeHeightAndNotJustTheGroove() {
            RectTransform slider = Child(_sliderRow, "Slider");
            RectTransform groove = Child(slider, "Track");
            Assert.Less(groove.rect.height, slider.rect.height - 8f,
                "the groove already fills the control, so this proves nothing");

            // left of the handle and above the groove: the corner of the control that draws nothing
            Tooltip tip = Hovered(PointOn(slider, 0.05f, 0.9f), out string seam);
            Assert.IsNotNull(tip, "the empty part of a slider raises nothing" + seam);
            Assert.AreEqual("Heuristic weight", tip.text, "the slider raises the wrong thing" + seam);
        }

        /// <summary>
        /// The dashed rule is the only thing advertising the row as hoverable, so it has to be the
        /// same width as the zone that answers. Two of these on one row would be a lie either way.
        /// </summary>
        [Test]
        public void TheDashedRuleIsAsWideAsTheZoneItMarks() {
            RectTransform rule = Child(Caption, "Underline");
            Assert.AreEqual(Zone.rect.width, rule.rect.width, 0.5f,
                "the rule marks a different width than the hover answers to");
            Assert.Greater(rule.rect.width, 0f, "the rule has no width, so this proves nothing");
        }

        /// <summary>
        /// A greyed control explains itself through the same tooltip, so the gate has to still be able
        /// to reach it and the control has to still be hoverable once it is closed.
        /// </summary>
        [Test]
        public void AClosedGateStillAnswersFromTheControl() {
            ControlGate gate = _row.gameObject.AddComponent<ControlGate>().Bind(Paths.Core.Control.Diagonal);
            gate.Set(false, "Not read by this search");

            Tooltip tip = Hovered(PointOn(Widget, 0.5f), out string seam);
            Assert.IsNotNull(tip, "a greyed control raises nothing, so it cannot say why" + seam);
            Assert.AreEqual("Not read by this search", tip.text, "the reason did not reach the control" + seam);
        }
    }
}
