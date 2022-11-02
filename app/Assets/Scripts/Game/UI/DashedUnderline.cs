using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// A thin dashed rule, drawn as one quad through UI/DashedLine so every dash is anti-aliased. Used as
    /// a quiet affordance under a label that carries a tooltip (the abbr[title] dotted-underline
    /// convention), so a field reads as "hover me for more" without a visible control.
    /// </summary>
    // a Graphic subclass doesn't inherit the base's RequireComponent, so ask for the renderer here
    [RequireComponent(typeof(CanvasRenderer))]
    public class DashedUnderline : MaskableGraphic {
        public float dash = 2f;
        public float gap = 2.5f;
        public float thickness = 1.25f;

        private static Material _shared;

        public override Material material {
            get {
                if (_shared == null)
                    _shared = new Material(Shader.Find("UI/DashedLine")) { name = "DashedLine (shared)" };
                return _shared;
            }
            set { }
        }

        /// <summary>
        /// How a run of whole dashes covers a width: how many, and how far apart. The remainder is
        /// spread across the gaps rather than left at the end, so the run finishes flush and both
        /// ends overhang by the same margin.
        /// </summary>
        public static void Fit(float width, float dash, float gap, out int count, out float spacing) {
            count = Mathf.Max(1, Mathf.FloorToInt((width + gap) / (dash + gap)));
            spacing = count > 1 ? (width - count * dash) / (count - 1) : gap;
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            Rect r = rectTransform.rect;
            Fit(r.width, dash, gap, out int count, out float spacing);
            float period = dash + spacing;
            float length = count > 1 ? r.width : Mathf.Min(dash, r.width);
            float radius = thickness * 0.5f;
            float centre = r.yMin + radius;
            // room for the AA falloff, but never so far that the quad reaches into the next phantom dash
            float margin = Mathf.Min(thickness + 1f, spacing * 0.9f);

            float x0 = r.xMin - margin, x1 = r.xMin + length + margin;
            float y0 = centre - radius - margin, y1 = centre + radius + margin;
            Vert(vh, x0, y0, r.xMin, centre, radius, period);
            Vert(vh, x0, y1, r.xMin, centre, radius, period);
            Vert(vh, x1, y1, r.xMin, centre, radius, period);
            Vert(vh, x1, y0, r.xMin, centre, radius, period);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        // the shader works in units of the rule's half thickness, where a dash is a capsule of radius 1
        private void Vert(VertexHelper vh, float x, float y, float xMin, float centre, float radius, float period) {
            var v = UIVertex.simpleVert;
            v.position = new Vector3(x, y);
            v.color = color;
            v.uv0 = new Vector4((x - xMin) / radius, (y - centre) / radius, dash / radius, period / radius);
            vh.AddVert(v);
        }
    }
}
