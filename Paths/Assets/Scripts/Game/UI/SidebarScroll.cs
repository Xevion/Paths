using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI {
    /// <summary>
    /// A minimal, eased wheel scroller for the (uniformly down-scaled) sidebar content. A plain ScrollRect
    /// miscomputes range when the content has a localScale, so this drives content.anchoredPosition.y
    /// against the scaled content height directly, easing toward the target each frame. Clipping is handled
    /// by a RectMask2D on the viewport.
    /// </summary>
    public class SidebarScroll : MonoBehaviour, IScrollHandler {
        public RectTransform content;
        public RectTransform viewport;
        public float scale = 1f;
        public float speed = 120f;
        public float smooth = 14f;

        private float _target;

        public void OnScroll(PointerEventData e) {
            float max = Mathf.Max(0f, content.rect.height * scale - viewport.rect.height);
            _target = Mathf.Clamp(_target - e.scrollDelta.y * speed, 0f, max);
        }

        private void Update() {
            float max = Mathf.Max(0f, content.rect.height * scale - viewport.rect.height);
            _target = Mathf.Clamp(_target, 0f, max);
            Vector2 p = content.anchoredPosition;
            float y = Mathf.MoveTowards(p.y, _target, Mathf.Max(1f, Mathf.Abs(_target - p.y)) * smooth * Time.unscaledDeltaTime);
            if (!Mathf.Approximately(y, p.y)) content.anchoredPosition = new Vector2(p.x, y);
        }
    }
}
