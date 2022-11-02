using Game.UI;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests {
    /// <summary>
    /// How the dashed rule under a tooltip-bearing label lays its dashes out. The rule is sized to
    /// the label's text, so a run that does not span its rect reads as clipped at the right, where
    /// the left is overhanging by the anti-aliasing margin and looks deliberate.
    /// </summary>
    public class UnderlineTests {
        private const float Dash = 2f;
        private const float Gap = 2.5f;

        private static float Span(int count, float dash, float spacing) =>
            count * dash + (count - 1) * spacing;

        /// <summary>
        /// The run ends where the label does. Flooring to whole dashes and keeping the design gap
        /// leaves up to a whole period unused, which is the reported few pixels.
        /// </summary>
        [TestCase(150f)]
        [TestCase(100f)]
        [TestCase(63.5f)]
        [TestCase(41.2f)]
        [TestCase(9f)]
        public void TheDashesSpanTheWholeWidth(float width) {
            DashedUnderline.Fit(width, Dash, Gap, out int count, out float spacing);
            Assert.Greater(count, 1, $"width {width} should fit more than one dash");
            Assert.AreEqual(width, Span(count, Dash, spacing), 1e-3f,
                $"the run falls {width - Span(count, Dash, spacing):0.00}px short of the label");
        }

        /// <summary>
        /// Absorbing the remainder must not visibly restripe the rule. At these sizes the leftover
        /// is under one period spread across every gap, so the change per gap is a fraction of one.
        /// </summary>
        [TestCase(150f)]
        [TestCase(100f)]
        [TestCase(63.5f)]
        [TestCase(41.2f)]
        public void TheGapsStayCloseToTheOneAskedFor(float width) {
            DashedUnderline.Fit(width, Dash, Gap, out _, out float spacing);
            Assert.GreaterOrEqual(spacing, Gap, "a stretched gap is never tighter than the design one");
            Assert.Less(spacing, Gap + 1f, $"gap stretched to {spacing:0.00} from {Gap}");
        }

        /// <summary>Whole dashes only, so neither end is cut through the middle of one.</summary>
        [TestCase(150f)]
        [TestCase(63.5f)]
        [TestCase(9f)]
        public void TheRunIsWholeDashes(float width) {
            DashedUnderline.Fit(width, Dash, Gap, out int count, out float spacing);
            Assert.GreaterOrEqual(count, 1);
            Assert.GreaterOrEqual(spacing, 0f, "dashes should not be made to overlap");
            Assert.LessOrEqual(count * Dash, width + 1e-3f, "the dashes alone already overflow");
        }

        /// <summary>
        /// A label too narrow for two dashes still draws one rather than dividing by zero, which is
        /// what the count of one guards.
        /// </summary>
        [TestCase(3f)]
        [TestCase(1f)]
        [TestCase(0f)]
        public void ARuleTooNarrowForTwoDashesStillDrawsOne(float width) {
            DashedUnderline.Fit(width, Dash, Gap, out int count, out float spacing);
            Assert.AreEqual(1, count);
            Assert.AreEqual(Gap, spacing, 1e-5f, "one dash has no gap to stretch, so it keeps the design one");
            Assert.IsFalse(float.IsNaN(spacing) || float.IsInfinity(spacing));
        }

        /// <summary>
        /// The rule is one quad phased by the shader, so the dash and period it carries have to be
        /// the ones actually laid out. Reading the fields back instead would restore the shortfall.
        /// </summary>
        [Test]
        public void TheMeshCarriesTheSpacingItWasLaidOutWith() {
            var go = new GameObject("Underline", typeof(RectTransform), typeof(CanvasRenderer));
            try {
                var rule = go.AddComponent<Probe>();
                rule.dash = Dash;
                rule.gap = Gap;
                ((RectTransform) go.transform).sizeDelta = new Vector2(150f, 1f);

                var vh = new UnityEngine.UI.VertexHelper();
                rule.Populate(vh);
                var vertex = new UIVertex();
                vh.PopulateUIVertex(ref vertex, 0);
                vh.Dispose();

                DashedUnderline.Fit(150f, Dash, Gap, out _, out float spacing);
                float radius = rule.thickness * 0.5f;
                Assert.AreEqual(Dash / radius, vertex.uv0.z, 1e-3f, "the mesh carries the wrong dash width");
                Assert.AreEqual((Dash + spacing) / radius, vertex.uv0.w, 1e-3f,
                    "the mesh carries a period the layout did not use");
            } finally {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>Reaches OnPopulateMesh, which is protected, without widening it in the kit.</summary>
        private class Probe : DashedUnderline {
            public void Populate(UnityEngine.UI.VertexHelper vh) => OnPopulateMesh(vh);
        }
    }
}
