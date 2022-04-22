using TMPro;
using UnityEngine;

namespace Game.UI {
    /// <summary>
    /// Spins the dropdown's chevron 180 degrees while the list is open and eases it back on close, so the
    /// trigger reflects its state. Runs on unscaled time so it animates regardless of the sim timescale.
    /// </summary>
    public class DropdownChevron : MonoBehaviour {
        public TMP_Dropdown dropdown;
        public RectTransform chevron;
        public float speed = 9f;

        private float _t;

        private void Update() {
            float target = dropdown.IsExpanded ? 1f : 0f;
            if (Mathf.Approximately(_t, target)) return;
            _t = Mathf.MoveTowards(_t, target, Time.unscaledDeltaTime * speed);
            chevron.localEulerAngles = new Vector3(0f, 0f, _t * 180f);
        }
    }

    /// <summary>
    /// Eases the popup list in when it opens. TMP fades the list's alpha but pops its size; this catches
    /// the freshly-instantiated "Dropdown List" child and grows it from a hair under full size so the open
    /// reads as a quick unfold paired with TMP's fade. No-ops once it's settled.
    /// </summary>
    public class DropdownPopupAnim : MonoBehaviour {
        public float speed = 9f;

        private RectTransform _list;
        private float _t;

        private void Update() {
            Transform found = transform.Find("Dropdown List");
            if (found == null) { _list = null; return; }
            if (found != _list) { _list = (RectTransform) found; _t = 0f; } // a new open
            _t = Mathf.MoveTowards(_t, 1f, Time.unscaledDeltaTime * speed);
            float e = _t * _t * (3f - 2f * _t); // smoothstep
            _list.localScale = new Vector3(1f, Mathf.Lerp(0.94f, 1f, e), 1f);
        }
    }
}
