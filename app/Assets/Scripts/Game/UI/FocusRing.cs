using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// A control that wants the focus ring on something other than its own rect (e.g. a segmented group
    /// pointing the ring at its focused segment) implements this. The ring follows the returned rect.
    /// </summary>
    public interface IFocusTarget {
        RectTransform FocusRect { get; }
    }

    /// <summary>
    /// Pulls the focus ring's bottom edge up by `amount` px on the control it's added to. Tabs carry one so
    /// the ring clears the accent underline pinned below them instead of fusing into it.
    /// </summary>
    public class FocusRingBottomInset : MonoBehaviour {
        public float amount = 7f;
    }

    /// <summary>
    /// Draws the accent ring through UI/FocusRing - geometry rides the mesh, the pulse rides the colour
    /// alpha (driven by FocusRing). Same vertex packing as RoundedRectGraphic; the mesh grows past the rect
    /// so the band can sit just outside the control's edge.
    /// </summary>
    public class FocusRingGraphic : MaskableGraphic {
        public float cornerRadius = Theme.RadiusSm;

        private static Material _shared;

        public override Material material {
            get {
                if (_shared == null)
                    _shared = new Material(Shader.Find("UI/FocusRing")) { name = "FocusRing (shared)" };
                return _shared;
            }
            set { }
        }

        protected override void OnEnable() {
            base.OnEnable();
            EnableExtraChannel();
        }

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
            const float margin = 9f; // room for gap + ring width + AA outside the rect

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
            var v = UIVertex.simpleVert;
            v.position = new Vector3(x, y);
            v.color = color;
            v.uv0 = new Vector4(x - center.x, y - center.y, 0f, 0f);
            v.uv1 = new Vector4(half.x, half.y, radius, 0f);
            vh.AddVert(v);
        }
    }

    /// <summary>
    /// A single accent ring that follows the EventSystem's currently-selected control and pulses softly, so
    /// keyboard focus and the last-clicked control are always visible. It reparents itself under the focused
    /// control (stretched to fill), matching its corner radius; the band draws just outside, so the control
    /// shows through. Hidden when nothing focusable is selected. Add one per UI canvas.
    /// </summary>
    public class FocusRing : MonoBehaviour {
        private FocusRingGraphic _ring;
        private GameObject _target;
        private float _phase;

        private void Awake() {
            EnsureRing();
        }

        // the ring lives parented under the focused control; when that control is a modal's and the modal is
        // destroyed, the ring goes with it. Rebuild it (Unity's == reports a destroyed object as null).
        private void EnsureRing() {
            if (_ring != null) return;
            var go = new GameObject("Ring", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _ring = go.AddComponent<FocusRingGraphic>();
            _ring.color = new Color(Theme.Accent.r, Theme.Accent.g, Theme.Accent.b, 0f);
            _ring.raycastTarget = false;
            // the ring parents into controls that have layout groups; keep it out of their layout so it
            // overlays (fill anchors) instead of being treated as a sized content child.
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            go.SetActive(false);
            _target = null;
        }

        private void LateUpdate() {
            EnsureRing();
            EventSystem es = EventSystem.current;
            GameObject sel = es != null ? es.currentSelectedGameObject : null;
            Selectable selectable = sel != null ? sel.GetComponent<Selectable>() : null;
            // skip the items inside an open dropdown popup, so closing it never flashes a giant ring over the list
            bool focusable = selectable != null && KeyboardFocus.IsFocusable(selectable);

            if (!focusable) {
                if (_ring.gameObject.activeSelf) _ring.gameObject.SetActive(false);
                _target = null;
                return;
            }

            var rt = (RectTransform) _ring.transform;
            var proxy = sel.GetComponent<IFocusTarget>();
            if (proxy != null) {
                // a segmented group: ride the focused segment, redrawn over its siblings (last sibling of the
                // group) and unclipped (maskable off) so the per-segment RectMask2D label clips don't eat the band
                RectTransform seg = proxy.FocusRect;
                var group = (RectTransform) seg.parent;
                if (rt.parent != group) rt.SetParent(group, false);
                rt.anchorMin = seg.anchorMin;
                rt.anchorMax = seg.anchorMax;
                rt.pivot = seg.pivot;
                rt.sizeDelta = seg.sizeDelta;
                rt.anchoredPosition = seg.anchoredPosition;
                rt.SetAsLastSibling();
                SetMaskable(false);
                var srr = seg.GetComponent<RoundedRectGraphic>();
                _ring.cornerRadius = srr != null ? srr.cornerRadius : Theme.RadiusSm;
                _target = sel;
                if (!_ring.gameObject.activeSelf) _ring.gameObject.SetActive(true);
            } else if (sel != _target) {
                _target = sel;
                rt.SetParent((RectTransform) sel.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                var inset = sel.GetComponent<FocusRingBottomInset>();
                rt.offsetMin = new Vector2(0f, inset != null ? inset.amount : 0f); // lift the bottom off a tab underline
                rt.offsetMax = Vector2.zero;
                rt.SetAsLastSibling();
                SetMaskable(true);
                var rr = sel.GetComponent<RoundedRectGraphic>();
                _ring.cornerRadius = rr != null ? rr.cornerRadius : Theme.RadiusSm;
                _ring.gameObject.SetActive(true);
            }

            _phase += Time.unscaledDeltaTime * 3f;
            Color c = _ring.color;
            c.a = 0.45f + 0.35f * Mathf.Sin(_phase); // gentle breathe, never fully off
            _ring.color = c;
        }

        private void SetMaskable(bool m) {
            if (_ring.maskable != m) _ring.maskable = m;
        }
    }
}
