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
    /// The app's pointer. Everything reads it through here, uGUI included, so there is one answer to
    /// where the cursor is and what it is doing rather than one per caller.
    /// </summary>
    public static class PointerInput {
        public static IPointerSource Source = new MousePointerSource();

        public static Vector2 Position => Source.Position;
        public static Vector2 ScrollDelta => Source.ScrollDelta;
        public static float ScrollAxis => Source.ScrollAxis;
        public static float DragDelta => Source.DragDelta;

        public static bool Held(int button = 0) => Source.Held(button);
        public static bool Pressed(int button = 0) => Source.Pressed(button);
        public static bool Released(int button = 0) => Source.Released(button);

        /// <summary>Whether the pointer is over a UI raycast target right now.</summary>
        public static bool OverUi =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        public static void WarpTo(Vector2 position) => Source.WarpTo(position);

        /// <summary>Go back to the real mouse.</summary>
        public static void Reset() => Source = new MousePointerSource();
    }
}
