using UnityEngine;

namespace Game.UI {
    /// <summary>
    /// Eases a Switch's knob slide and track-colour crossfade when toggled, instead of snapping. Runs on
    /// unscaled time so it animates regardless of the sim's timescale.
    /// </summary>
    public class UISwitch : MonoBehaviour {
        public RectTransform knob;
        public RoundedRectGraphic knobGraphic;
        public RoundedRectGraphic track;
        public float offX = 13f;
        public float onX = 33f;

        private bool _on;
        private float _t;

        public void Init(bool on) {
            _on = on;
            _t = on ? 1f : 0f;
            Apply();
        }

        public void Set(bool on) {
            _on = on;
        }

        private void Update() {
            float target = _on ? 1f : 0f;
            if (Mathf.Approximately(_t, target)) return;
            _t = Mathf.MoveTowards(_t, target, Time.unscaledDeltaTime / 0.12f);
            Apply();
        }

        private void Apply() {
            float e = _t * _t * (3f - 2f * _t); // smoothstep
            knob.anchoredPosition = new Vector2(Mathf.Lerp(offX, onX, e), 0f);
            track.color = Color.Lerp(Theme.Track, Theme.Accent, e);
            // white knob on the grey track when off, dark knob on the cyan track when on - keeps contrast
            // either way instead of a low-contrast white-on-cyan when active
            if (knobGraphic != null) knobGraphic.color = Color.Lerp(Color.white, Theme.AccentText, e);
        }
    }
}
