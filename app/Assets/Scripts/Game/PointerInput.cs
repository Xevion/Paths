using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game {
    /// <summary>Where pointer state comes from. Swappable so tests can drive the app without a mouse.</summary>
    public interface IPointerSource {
        Vector2 Position { get; }
        /// <summary>Wheel notches, the scale uGUI's scroll handlers expect.</summary>
        Vector2 ScrollDelta { get; }
        /// <summary>The legacy wheel axis, which camera zoom is tuned against.</summary>
        float ScrollAxis { get; }
        /// <summary>Raw horizontal movement, for controls that want delta and not position.</summary>
        float DragDelta { get; }
        bool Held(int button);
        bool Pressed(int button);
        bool Released(int button);
        /// <summary>Put the hardware cursor somewhere. Not every platform allows it.</summary>
        void WarpTo(Vector2 position);
    }

    public class MousePointerSource : IPointerSource {
        public Vector2 Position => Input.mousePosition;
        public Vector2 ScrollDelta => Input.mouseScrollDelta;
        public float ScrollAxis => Input.GetAxis("Mouse ScrollWheel");
        public float DragDelta => Input.GetAxisRaw("Mouse X");
        public bool Held(int button) => Input.GetMouseButton(button);
        public bool Pressed(int button) => Input.GetMouseButtonDown(button);
        public bool Released(int button) => Input.GetMouseButtonUp(button);

        public void WarpTo(Vector2 position) {
            if (Mouse.current != null) Mouse.current.WarpCursorPosition(position);
        }
    }

    /// <summary>
    /// The app's pointer. A control that drags with the cursor locked pins the position, since
    /// locking parks the hardware cursor but not the position the app reads. A press belongs to
    /// whatever it started on, so one that began on the interface never reaches the grid.
    /// </summary>
    public static class PointerInput {
        public static IPointerSource Source = new MousePointerSource();

        private static bool _pinned;
        private static Vector2 _anchor;
        private static readonly bool[] _ui = new bool[3];

        public static Vector2 Position => _pinned ? _anchor : Source.Position;
        public static Vector2 ScrollDelta => Source.ScrollDelta;
        public static float ScrollAxis => Source.ScrollAxis;
        public static float DragDelta => Source.DragDelta;
        public static bool Pinned => _pinned;

        public static bool Held(int button = 0) => Source.Held(button);
        public static bool Pressed(int button = 0) => Source.Pressed(button);
        public static bool Released(int button = 0) => Source.Released(button);

        /// <summary>Whether the pointer is over a UI raycast target right now.</summary>
        public static bool OverUi =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        /// <summary>Whether the press in progress began on the UI. Holds for the whole gesture.</summary>
        public static bool UiOwns(int button = 0) => _ui[button];

        /// <summary>A press the world is free to act on: held, and it did not begin on the UI.</summary>
        public static bool Grabbed(int button = 0) => Source.Held(button) && !_ui[button];

        /// <summary>Freeze the reported position at a point until <see cref="Unpin"/>.</summary>
        public static void Pin(Vector2 at) {
            _pinned = true;
            _anchor = at;
        }

        public static void Unpin() => _pinned = false;

        public static void WarpTo(Vector2 position) => Source.WarpTo(position);

        /// <summary>Latch press ownership. Runs once a frame, ahead of anything that reads it.</summary>
        public static void Update() {
            for (int b = 0; b < _ui.Length; b++) {
                if (Source.Pressed(b))
                    _ui[b] = OverUi;
                else if (!Source.Held(b))
                    _ui[b] = false;
            }
        }

        /// <summary>Drop all latched state and go back to the real mouse.</summary>
        public static void Reset() {
            Source = new MousePointerSource();
            _pinned = false;
            for (int b = 0; b < _ui.Length; b++) _ui[b] = false;
        }
    }
}
