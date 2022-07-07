using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI {
    public enum CursorKind { Arrow, Hand, ResizeH, Text }

    /// <summary>
    /// Swaps the hardware cursor to match what's under it - a hand over clickables, a horizontal-resize
    /// arrow over sliders/scrub fields, an I-beam over text. Cursors are tiny code-drawn pixel bitmaps so
    /// nothing has to be vendored. A small reference count keeps overlapping hovers from fighting.
    /// </summary>
    public static class Cursors {
        private static readonly Dictionary<CursorKind, Texture2D> _tex = new Dictionary<CursorKind, Texture2D>();
        private static readonly Dictionary<CursorKind, Vector2> _hot = new Dictionary<CursorKind, Vector2>();
        private static CursorKind _current = CursorKind.Arrow;
        private static int _depth;

        public static void Push(CursorKind kind) {
            _depth++;
            Set(kind);
        }

        public static void Pop() {
            _depth--;
            if (_depth <= 0) {
                _depth = 0;
                Set(CursorKind.Arrow);
            }
        }

        private static void Set(CursorKind kind) {
            if (kind == _current) return;
            _current = kind;
            if (kind == CursorKind.Arrow) {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                return;
            }
            Build(kind);
            Cursor.SetCursor(_tex[kind], _hot[kind], CursorMode.Auto);
        }

        private static readonly Dictionary<CursorKind, Sprite> _sprite = new Dictionary<CursorKind, Sprite>();

        /// <summary>A UI sprite of a cursor's bitmap, for drawing a frozen cursor while one is locked.</summary>
        public static Sprite SpriteFor(CursorKind kind) {
            if (_sprite.TryGetValue(kind, out Sprite s) && s != null) return s;
            Build(kind);
            Texture2D tex = _tex[kind];
            s = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), Vector2.zero, 1f, 0,
                SpriteMeshType.FullRect);
            _sprite[kind] = s;
            return s;
        }

        /// <summary>The cursor's hotspot in texture pixels (top-left origin), for aligning a frozen draw.</summary>
        public static Vector2 Hotspot(CursorKind kind) {
            Build(kind);
            return _hot.TryGetValue(kind, out Vector2 h) ? h : Vector2.zero;
        }

        private static void Build(CursorKind kind) {
            if (_tex.ContainsKey(kind)) return;
            string[] art;
            Vector2 hot;
            switch (kind) {
                case CursorKind.Hand: art = Hand; hot = new Vector2(4f, 0f); break;
                case CursorKind.Text: art = Beam; hot = new Vector2(4f, 8f); break;
                default: art = ResizeH; hot = new Vector2(8f, 4f); break;
            }
            int h = art.Length, w = art[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) {
                    char c = art[h - 1 - y][x]; // texture origin is bottom-left; art is top-down
                    tex.SetPixel(x, y, c == 'W' ? Color.white : c == 'B' ? Color.black : clear);
                }
            tex.Apply();
            _tex[kind] = tex;
            _hot[kind] = hot;
        }

        // pointing hand, index finger up - hotspot at the fingertip
        private static readonly string[] Hand = {
            "...BB........",
            "..BWWB.......",
            "..BWWB.......",
            "..BWWB.......",
            "..BWWB.......",
            "..BWWBBB.....",
            "..BWWBWWBB...",
            "..BWWBWWBWB..",
            "..BWWBWWBWWB.",
            "BBBWWWWWWWWWB",
            "BWWWWWWWWWWWB",
            "BWWWWWWWWWWWB",
            ".BWWWWWWWWWWB",
            ".BWWWWWWWWWB.",
            "..BWWWWWWWWB.",
            "..BWWWWWWWB..",
            "...BWWWWWWB..",
            "...BWWWWWWB..",
            "...BWWWWWWB..",
            "...BBBBBBBB..",
        };

        // horizontal double-arrow for drag-to-change controls
        private static readonly string[] ResizeH = {
            ".....B....B.....",
            "....BB....BB....",
            "...BWB....BWB...",
            "..BWWBBBBBBWWB..",
            ".BWWWWWWWWWWWWB.",
            "..BWWBBBBBBWWB..",
            "...BWB....BWB...",
            "....BB....BB....",
            ".....B....B.....",
        };

        private static readonly string[] Beam = {
            "BBB.BBB",
            "..BWB..",
            "..BWB..",
            "..BWB..",
            "..BWB..",
            "..BWB..",
            "..BWB..",
            "..BWB..",
            "..BWB..",
            "..BWB..",
            "..BWB..",
            "BBB.BBB",
        };
    }

    /// <summary>
    /// A single overlay cursor pinned at a fixed screen point, drawn while a control locks the hardware
    /// cursor (which hides it). Lets a scrub keep the cursor visibly parked where you pressed instead of
    /// vanishing. Constant-pixel canvas so screen coords map straight to its position.
    /// </summary>
    public class FrozenCursor : MonoBehaviour {
        private static FrozenCursor _instance;
        public static FrozenCursor Instance => _instance != null ? _instance : (_instance = Build());

        private RectTransform _img;
        private Vector2 _hotspot;
        private float _height;

        private static FrozenCursor Build() {
            var go = new GameObject("Frozen Cursor");
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 31000; // above the dropdown popup, below the tooltip layer
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var fc = go.AddComponent<FrozenCursor>();

            Sprite sprite = Cursors.SpriteFor(CursorKind.ResizeH);
            var cgo = new GameObject("Cursor", typeof(RectTransform));
            var rt = (RectTransform) cgo.transform;
            rt.SetParent(go.transform, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero; // bottom-left origin = raw screen pixels
            rt.sizeDelta = sprite.rect.size;
            var img = cgo.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            cgo.SetActive(false);

            fc._img = rt;
            fc._hotspot = Cursors.Hotspot(CursorKind.ResizeH);
            fc._height = sprite.rect.height;
            return fc;
        }

        public void Show(Vector2 screen) {
            // hotspot is top-left origin; the rect is bottom-left, so flip the y offset
            _img.anchoredPosition = new Vector2(screen.x - _hotspot.x, screen.y - (_height - _hotspot.y));
            _img.gameObject.SetActive(true);
        }

        public void Hide() {
            if (_img != null) _img.gameObject.SetActive(false);
        }
    }

    /// <summary>Attach to a raycast target to switch the cursor while hovered.</summary>
    public class HoverCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public CursorKind kind = CursorKind.Hand;
        private bool _inside;

        public void OnPointerEnter(PointerEventData e) {
            _inside = true;
            Cursors.Push(kind);
        }

        public void OnPointerExit(PointerEventData e) {
            if (!_inside) return;
            _inside = false;
            Cursors.Pop();
        }

        private void OnDisable() {
            if (!_inside) return;
            _inside = false;
            Cursors.Pop();
        }
    }
}
