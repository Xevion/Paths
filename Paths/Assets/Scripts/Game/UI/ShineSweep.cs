using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// A specular sheen that sweeps across a button on hover - a thin, tilted highlight band clipped to the
    /// button by a RectMask2D, sliding left-to-right and fading in at the middle of the pass. Tactile, cheap,
    /// no shader work. Call Build() after the button's graphic + layout are set up.
    /// </summary>
    public class ShineSweep : MonoBehaviour, IPointerEnterHandler {
        public float duration = 0.5f;
        public float peakAlpha = 0.13f;

        private RectTransform _band;
        private Image _img;
        private float _t = 2f; // >1 = idle

        public void Build() {
            // mask on a child clip, NOT the button - a mask on the button would clip the focus ring, which
            // parents under the focused control and draws just outside its rect.
            var clipGo = new GameObject("ShineClip", typeof(RectTransform));
            var clip = (RectTransform) clipGo.transform;
            clip.SetParent(transform, false);
            clip.anchorMin = Vector2.zero;
            clip.anchorMax = Vector2.one;
            clip.offsetMin = clip.offsetMax = Vector2.zero;
            clipGo.AddComponent<LayoutElement>().ignoreLayout = true; // overlay, not a layout child
            clipGo.AddComponent<RectMask2D>();

            var go = new GameObject("Shine", typeof(RectTransform));
            _band = (RectTransform) go.transform;
            _band.SetParent(clip, false);
            _band.anchorMin = new Vector2(0.5f, 0f);
            _band.anchorMax = new Vector2(0.5f, 1f);
            _band.pivot = new Vector2(0.5f, 0.5f);
            _band.sizeDelta = new Vector2(16f, 28f); // fixed width, overshoot the height so the tilt covers
            _band.localRotation = Quaternion.Euler(0f, 0f, 16f);
            _img = go.AddComponent<Image>();
            _img.color = new Color(1f, 1f, 1f, 0f);
            _img.raycastTarget = false;
        }

        public void OnPointerEnter(PointerEventData e) => _t = 0f;

        private void Update() {
            if (_t > 1f) return;
            _t += Time.unscaledDeltaTime / duration;
            float e = Mathf.Clamp01(_t);
            float w = ((RectTransform) transform).rect.width;
            _band.anchoredPosition = new Vector2(Mathf.Lerp(-w * 0.7f, w * 0.7f, e), 0f);
            Color c = _img.color;
            c.a = Mathf.Sin(e * Mathf.PI) * peakAlpha; // up at the middle of the pass, down at the ends
            _img.color = c;
        }
    }
}
