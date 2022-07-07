using UnityEngine;

namespace Game.UI {
    /// <summary>
    /// Slides a panel on/off by translating it (and fading it), so it overlays the scene rather than
    /// pushing it - the grid behind stays put when the sidebar opens or closes. Runs on unscaled time.
    /// </summary>
    public class PanelSlide : MonoBehaviour {
        public RectTransform panel;
        public CanvasGroup group;
        public float closedOffset = 280f; // how far to translate (in x) when closed
        public float speed = 8f;

        private bool _open = true;
        private float _t = 1f;

        public void Toggle() => _open = !_open;

        private void Update() {
            float to = _open ? 1f : 0f;
            if (Mathf.Approximately(_t, to)) return;
            _t = Motion.Reduced ? to : Mathf.MoveTowards(_t, to, Time.unscaledDeltaTime * speed);
            float e = _t * _t * (3f - 2f * _t); // smoothstep
            Vector2 p = panel.anchoredPosition;
            p.x = closedOffset * (1f - e);
            panel.anchoredPosition = p;
            if (group != null) {
                group.alpha = e;
                group.blocksRaycasts = _open;
            }
        }
    }
}
