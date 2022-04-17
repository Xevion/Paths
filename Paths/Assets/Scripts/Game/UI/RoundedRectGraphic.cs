using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// A uGUI graphic that draws a rounded rectangle (with anti-aliased edges, a hairline border and
    /// an optional soft shadow) through the UI/RoundedRect shader. Per-panel size, radius and shadow
    /// ride in the mesh's vertex channels, so every panel shares one material and one draw setup.
    /// </summary>
    public class RoundedRectGraphic : MaskableGraphic {
        public float cornerRadius = Theme.RadiusMd;
        public float shadowSize;

        private static Material _shared;

        public override Material material {
            get {
                if (_shared == null)
                    _shared = new Material(Shader.Find("UI/RoundedRect")) { name = "RoundedRect (shared)" };
                return _shared;
            }
            set { } // shared material is fixed; ignore per-instance assignment
        }

        protected override void OnEnable() {
            base.OnEnable();
            EnableExtraChannel();
        }

        // the shader reads per-panel geometry from TexCoord1, which the canvas strips unless asked
        private void EnableExtraChannel() {
            if (canvas != null)
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        }

        protected override void OnCanvasHierarchyChanged() {
            base.OnCanvasHierarchyChanged();
            EnableExtraChannel();
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();

            Rect rect = rectTransform.rect;
            Vector2 center = rect.center;
            Vector2 half = rect.size * 0.5f;
            float radius = Mathf.Min(cornerRadius, Mathf.Min(half.x, half.y));

            // grow the mesh past the visual rect so the shadow has room to fall outside it
            float margin = shadowSize + 6f;
            float xMin = rect.xMin - margin, xMax = rect.xMax + margin;
            float yMin = rect.yMin - margin, yMax = rect.yMax + margin;

            AddVert(vh, xMin, yMin, center, half, radius);
            AddVert(vh, xMin, yMax, center, half, radius);
            AddVert(vh, xMax, yMax, center, half, radius);
            AddVert(vh, xMax, yMin, center, half, radius);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        private void AddVert(VertexHelper vh, float x, float y, Vector2 center, Vector2 half, float radius) {
            var vert = UIVertex.simpleVert;
            vert.position = new Vector3(x, y);
            vert.color = color;
            vert.uv0 = new Vector4(x - center.x, y - center.y, 0f, 0f);
            vert.uv1 = new Vector4(half.x, half.y, radius, shadowSize);
            vh.AddVert(vert);
        }
    }
}
