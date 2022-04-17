using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// A thin dashed rule, drawn as a row of dash quads in one mesh. Used as a quiet affordance under a
    /// label that carries a tooltip (the abbr[title] dotted-underline convention), so a field reads as
    /// "hover me for more" without a visible control.
    /// </summary>
    public class DashedUnderline : MaskableGraphic {
        // the sidebar content is downscaled, so sub-1px geometry shimmers - keep the dashes a hair chunky
        // (short dash + wider gap still reads as dots) and the rule ~1.5px so it stays solid after the scale
        public float dash = 2f;
        public float gap = 3f;
        public float thickness = 1.5f;

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            Rect r = rectTransform.rect;
            float x = r.xMin;
            while (x < r.xMax) {
                float x1 = Mathf.Min(x + dash, r.xMax);
                Quad(vh, x, r.yMin, x1, r.yMin + thickness);
                x += dash + gap;
            }
        }

        private void Quad(VertexHelper vh, float x0, float y0, float x1, float y1) {
            int i = vh.currentVertCount;
            var v = UIVertex.simpleVert;
            v.color = color;
            v.position = new Vector3(x0, y0); vh.AddVert(v);
            v.position = new Vector3(x0, y1); vh.AddVert(v);
            v.position = new Vector3(x1, y1); vh.AddVert(v);
            v.position = new Vector3(x1, y0); vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i + 2, i + 3, i);
        }
    }
}
