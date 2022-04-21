using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// One focusable segmented/radio group: a single Tab stop where left/right arrows move the focus between
    /// segments and Space/Enter commits the focused one (clicking a segment commits it outright). The segments
    /// themselves aren't Selectables, so they don't each become a Tab stop; the control hands the focused
    /// segment to the FocusRing through IFocusTarget so the ring lands on - and slides between - segments.
    /// </summary>
    public class SegmentedControl : Selectable, ISubmitHandler, IFocusTarget {
        public RectTransform[] segments;
        public Action<int> visuals;  // repaint segment colours/labels for the committed index
        public Action<int> animate;  // optional width ease (SegmentedExpand); null for the plain row
        public Action<int> onChange;

        private int _current;
        private int _focus;

        public void Init(int current) { _current = _focus = current; }
        public bool IsCurrent(int i) => i == _current;

        public override void OnSelect(BaseEventData e) {
            base.OnSelect(e);
            _focus = _current; // focus enters on the active segment
        }

        public override void OnMove(AxisEventData e) {
            switch (e.moveDir) {
                case MoveDirection.Left: _focus = Mathf.Max(0, _focus - 1); e.Use(); return;
                case MoveDirection.Right: _focus = Mathf.Min(segments.Length - 1, _focus + 1); e.Use(); return;
            }
            base.OnMove(e); // up/down: navigation is none, so this no-ops
        }

        public void OnSubmit(BaseEventData e) => Commit(_focus);

        // a segment was clicked: focus it, select this control so the ring shows, and commit
        public void Pick(int index) {
            _focus = index;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
            Commit(index);
        }

        private void Commit(int index) {
            if (index == _current) return;
            _current = index;
            visuals(index);
            animate?.Invoke(index);
            onChange(index);
        }

        public RectTransform FocusRect =>
            segments != null && _focus >= 0 && _focus < segments.Length ? segments[_focus] : (RectTransform) transform;
    }
}
