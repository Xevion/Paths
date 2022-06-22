using UnityEngine;

namespace Game.UI {
    /// <summary>
    /// A scale + fade for a panel - it grows from a hair under full size and fades in on open, reverses on
    /// Close(). Runs on unscaled time. The owner must force a layout rebuild before this runs so the panel's
    /// content-fit size is settled; otherwise the first frame's resize reads as a flicker on top of the scale.
    /// </summary>
    public class PopIn : MonoBehaviour {
        public RectTransform scaleTarget;
        public CanvasGroup group;
        public float duration = 0.16f;

        private float _t;
        private int _dir = 1; // +1 opening, -1 closing
        private System.Action _onClosed;

        private void OnEnable() {
            _t = 0f;
            _dir = 1;
        }

        /// <summary>
        /// Snap to the start state (collapsed + transparent) right after the owner sets scaleTarget/group.
        /// OnEnable runs at AddComponent time, before those fields are assigned, so without this the panel
        /// renders one frame at full size/alpha before the animation takes over - the open "flicker".
        /// </summary>
        public void Begin() {
            _t = 0f;
            _dir = 1;
            Apply();
        }

        /// <summary>Play the animation in reverse, then invoke onClosed (e.g. Destroy the modal).</summary>
        public void Close(System.Action onClosed) {
            _onClosed = onClosed;
            _dir = -1;
        }

        private void Update() {
            if (_dir > 0 && _t >= 1f) return;
            float step = Motion.Reduced ? 1f : Time.unscaledDeltaTime / duration;
            _t = Mathf.Clamp01(_t + _dir * step);
            Apply();
            if (_dir < 0 && _t <= 0f && _onClosed != null) {
                System.Action done = _onClosed;
                _onClosed = null;
                done();
            }
        }

        private void Apply() {
            float e = _t * _t * (3f - 2f * _t); // smoothstep
            if (scaleTarget != null) scaleTarget.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, e);
            if (group != null) group.alpha = e;
        }
    }
}
