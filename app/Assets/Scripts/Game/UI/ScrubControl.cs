using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// Drag-to-edit (and scroll-to-edit, and arrow-key-to-edit) for an integer field: press and drag
    /// horizontally to nudge the value, scroll for single steps, or focus it and use the arrows. It's a
    /// Selectable so Tab can reach it and the focus ring can follow it. The press locks the cursor (which
    /// hides it) and reads raw mouse delta each frame, so the scrub keeps going past the window edge; a
    /// frozen cursor is drawn at the press point, and on release the OS cursor is warped back there.
    /// </summary>
    public class ScrubControl : Selectable, IScrollHandler {
        public TMP_Text label;
        public int value, min, max;
        public string suffix = "";
        public Action<int> onChange;
        // raw mouse-X units per single increment (the legacy axis is sensitivity-scaled, not pixels)
        public float unitsPerStep = 0.4f;
        public int coarseStep = 10; // up/down key step; left/right step by 1

        private bool _dragging;
        private bool _skipFrame;
        private bool _warpPending;
        private float _accum;
        private Vector2 _pressPoint;

        public override void OnPointerDown(PointerEventData e) {
            base.OnPointerDown(e); // selects the field, so the focus ring picks it up
            // capture the press point before the lock warps the OS cursor, then park a visible cursor there
            _pressPoint = Input.mousePosition;
            FrozenCursor.Instance.Show(_pressPoint);
            // lock on DOWN (not up) - browsers only grant Pointer Lock inside the gesture
            _dragging = true;
            _skipFrame = true; // the first locked frame reports a bogus jump-to-centre delta
            _accum = 0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public override void OnPointerUp(PointerEventData e) {
            base.OnPointerUp(e);
            Release();
        }

        protected override void OnDisable() {
            base.OnDisable();
            Release();
        }

        private void Release() {
            if (!_dragging) return;
            _dragging = false;
            Cursor.lockState = CursorLockMode.None;
            // a warp is ignored in the same frame the cursor unlocks (it's still re-centring), so defer it a
            // frame while the control is live; if we're being disabled there's no next Update, so warp now
            if (isActiveAndEnabled) {
                _warpPending = true;
            } else {
                Warp();
            }
        }

        private void Warp() {
            if (Mouse.current != null) Mouse.current.WarpCursorPosition(_pressPoint);
            Cursor.visible = true;
            FrozenCursor.Instance.Hide();
        }

        private void Update() {
            if (_warpPending) {
                _warpPending = false;
                Warp();
                return;
            }
            if (!_dragging) return;
            // while the press holds the cursor locked, scroll events stop being routed here (the pointer is
            // pinned), so read the wheel straight off Input to keep scroll-to-step working mid-drag
            float wheel = Input.mouseScrollDelta.y;
            if (wheel != 0f) Set(value + (wheel > 0f ? 1 : -1));
            if (_skipFrame) { _skipFrame = false; return; }
            _accum += Input.GetAxisRaw("Mouse X");
            int steps = (int) (_accum / unitsPerStep);
            if (steps == 0) return;
            _accum -= steps * unitsPerStep;
            Set(value + steps);
        }

        public void OnScroll(PointerEventData e) {
            if (e.scrollDelta.y != 0f) Set(value + (e.scrollDelta.y > 0f ? 1 : -1));
        }

        // arrows adjust the focused field: left/right by 1, up/down by the coarse step
        public override void OnMove(AxisEventData e) {
            switch (e.moveDir) {
                case MoveDirection.Left: Set(value - 1); break;
                case MoveDirection.Right: Set(value + 1); break;
                case MoveDirection.Up: Set(value + coarseStep); break;
                case MoveDirection.Down: Set(value - coarseStep); break;
            }
        }

        public void Render() {
            label.text = value + suffix;
        }

        private void Set(int v) {
            v = Mathf.Clamp(v, min, max);
            if (v == value) return;
            value = v;
            Render();
            onChange?.Invoke(v);
        }
    }
}
