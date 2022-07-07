using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// Tab / Shift-Tab cycle the interactable controls in visual order (top-to-bottom, then left-to-right),
    /// setting the EventSystem selection so the focus ring follows. uGUI's input module never binds Tab, so
    /// without this keyboard focus has no entry point. A modal instance scopes to its own subtree and parks
    /// every other instance while it's open, so focus can't escape the dialog. Add one per UI root.
    /// </summary>
    public class KeyboardFocus : MonoBehaviour {
        public RectTransform root; // if set, only controls under this subtree are reachable

        [SerializeField] private bool _modal;

        /// <summary>
        /// A modal owns the keyboard: non-modal instances pause while one is open. This is assigned right
        /// after AddComponent (so OnEnable has already run with it false) - hence the count is adjusted in the
        /// setter, not OnEnable alone, or the desktop instance never parks and both fight over the selection.
        /// </summary>
        public bool modal {
            get => _modal;
            set {
                if (_modal == value) return;
                if (_modal && isActiveAndEnabled) _modalCount = Mathf.Max(0, _modalCount - 1);
                _modal = value;
                if (_modal && isActiveAndEnabled) _modalCount++;
            }
        }

        private static int _modalCount;
        private readonly List<Selectable> _buf = new List<Selectable>();

        /// <summary>
        /// A control is a focus stop when it's interactable and on screen, but NOT when it's an item inside an
        /// open dropdown popup - the dropdown is one stop, and its option list owns its own highlight. Shared
        /// with FocusRing so the ring and the Tab order agree on what's focusable.
        /// </summary>
        public static bool IsFocusable(Selectable s) {
            if (s == null || !s.IsInteractable() || !s.gameObject.activeInHierarchy) return false;
            // a popup option Toggle sits under a TMP_Dropdown but isn't one itself
            if (s.GetComponent<TMP_Dropdown>() == null && s.GetComponentInParent<TMP_Dropdown>() != null) return false;
            return true;
        }

        /// <summary>
        /// Swap geometric arrow-navigation for explicit-with-no-neighbours on every control under root. Clicks
        /// still select (uGUI only skips that when the mode is None), but the arrows find no neighbour and so
        /// move nothing - leaving them free to adjust whatever is focused (sliders, scrub fields, segments).
        /// </summary>
        public static void DisableArrowNav(GameObject root) {
            foreach (Selectable s in root.GetComponentsInChildren<Selectable>(true)) {
                Navigation nav = s.navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = nav.selectOnDown = nav.selectOnLeft = nav.selectOnRight = null;
                s.navigation = nav;
            }
        }

        private void OnEnable() {
            if (_modal) _modalCount++;
        }

        private void OnDisable() {
            if (_modal) _modalCount = Mathf.Max(0, _modalCount - 1);
        }

        private void Update() {
            if (!Input.GetKeyDown(KeyCode.Tab)) return;
            if (!modal && _modalCount > 0) return; // a modal has the keyboard
            EventSystem es = EventSystem.current;
            if (es == null) return;

            // Tab out of an open dropdown: close the popup (TMP's Hide re-selects the trigger) and carry on to
            // the next control instead of diving into the option list.
            TMP_Dropdown open = OpenDropdown();
            if (open != null) open.Hide();

            Collect();
            if (_buf.Count == 0) return;
            // visual order: top row first (higher screen y), then left-to-right within a row. Bucket y into
            // rows so the comparator is a real total order - a raw "within 5px = equal" test isn't transitive
            // and List.Sort throws on it once the set is big enough.
            _buf.Sort((a, b) => {
                Vector3 pa = a.transform.position, pb = b.transform.position;
                int ra = Mathf.RoundToInt(pa.y / 8f), rb = Mathf.RoundToInt(pb.y / 8f);
                return ra != rb ? rb.CompareTo(ra) : pa.x.CompareTo(pb.x);
            });

            bool back = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            GameObject cur = es.currentSelectedGameObject;
            int idx = _buf.FindIndex(s => s.gameObject == cur);
            int next = idx < 0 ? 0 : (idx + (back ? -1 : 1) + _buf.Count) % _buf.Count;
            es.SetSelectedGameObject(_buf[next].gameObject);
        }

        private TMP_Dropdown OpenDropdown() {
            Transform scope = root != null ? (Transform) root : transform;
            foreach (TMP_Dropdown d in scope.GetComponentsInChildren<TMP_Dropdown>(false))
                if (d.IsExpanded) return d;
            return null;
        }

        private void Collect() {
            _buf.Clear();
            if (root != null) {
                foreach (Selectable s in root.GetComponentsInChildren<Selectable>(false))
                    if (IsFocusable(s)) _buf.Add(s);
                return;
            }
            foreach (Selectable s in Selectable.allSelectablesArray)
                if (IsFocusable(s)) _buf.Add(s);
        }
    }
}
