using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// A static, decorative pathfinding picture for the desktop showcase backdrop: a cell grid with a
    /// start, an end, a corridor of "seen" and "frontier" cells around a solved path, and a few walls.
    /// Built procedurally in one mesh (cells inset by a hairline so the gaps read as grid lines), so it
    /// looks like the real app without running a search. Replace with the live grid on integration.
    /// </summary>
    public class FauxGrid : MaskableGraphic {
        public float cellSize = 26f;
        public float gap = 1.5f;

        private static readonly Color Base = Color.Lerp(Theme.Window, Theme.Surface, 0.22f);
        private static readonly Color Wall = Color.Lerp(Theme.Window, Theme.Border, 0.7f);
        private static readonly Color Seen = new Color(Theme.Accent.r, Theme.Accent.g, Theme.Accent.b, 0.14f);
        private static readonly Color Frontier = new Color(Theme.Accent.r, Theme.Accent.g, Theme.Accent.b, 0.34f);
        private static readonly Color Path = Theme.Accent;
        private static readonly Color Start = new Color(0.45f, 0.82f, 0.5f);
        private static readonly Color End = Theme.Danger;

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            Rect rect = rectTransform.rect;
            int cols = Mathf.Max(8, Mathf.RoundToInt(rect.width / cellSize));
            int rows = Mathf.Max(6, Mathf.RoundToInt(rect.height / cellSize));
            float cw = rect.width / cols;
            float ch = rect.height / rows;

            int sx = 2, sy = rows / 2;
            int ex = cols - 3, ey = rows / 2 - rows / 5;

            for (int cy = 0; cy < rows; cy++)
                for (int cx = 0; cx < cols; cx++) {
                    Color c = CellColor(cx, cy, cols, rows, sx, sy, ex, ey);
                    float x0 = rect.xMin + cx * cw + gap;
                    float y0 = rect.yMin + cy * ch + gap;
                    Quad(vh, x0, y0, x0 + cw - gap * 2f, y0 + ch - gap * 2f, c);
                }
        }

        private Color CellColor(int cx, int cy, int cols, int rows, int sx, int sy, int ex, int ey) {
            if (cx == sx && cy == sy) return Start;
            if (cx == ex && cy == ey) return End;

            // a gentle path from start to end with a couple of bends
            float t = Mathf.InverseLerp(sx, ex, cx);
            float pathY = Mathf.Lerp(sy, ey, t) + Mathf.Sin(t * 6.2f) * 1.6f;
            float dist = Mathf.Abs(cy - pathY);

            // a few wall pillars with a gap the path threads through
            bool wall = cx % 8 == 5 && Mathf.Abs(cy - Mathf.RoundToInt(pathY)) > 1 && (cy % 6 != 0);
            if (wall) return Wall;

            if (cx >= sx && cx <= ex) {
                if (dist < 0.5f) return Path;
                if (dist < 1.6f) return Frontier;
                if (dist < 3.4f) return Seen;
            }
            return Base;
        }

        private void Quad(VertexHelper vh, float x0, float y0, float x1, float y1, Color c) {
            int i = vh.currentVertCount;
            var v = UIVertex.simpleVert;
            v.color = c;
            v.position = new Vector3(x0, y0); vh.AddVert(v);
            v.position = new Vector3(x0, y1); vh.AddVert(v);
            v.position = new Vector3(x1, y1); vh.AddVert(v);
            v.position = new Vector3(x1, y0); vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i + 2, i + 3, i);
        }
    }
}
