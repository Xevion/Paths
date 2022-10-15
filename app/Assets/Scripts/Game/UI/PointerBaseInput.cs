using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI {
    /// <summary>
    /// Feeds uGUI from <see cref="PointerInput"/> so the event system and the rest of the app agree
    /// on where the pointer is. Tests drive the interface through the same seam.
    /// </summary>
    public class PointerBaseInput : BaseInput {
        public override Vector2 mousePosition => PointerInput.Position;
        public override Vector2 mouseScrollDelta => PointerInput.ScrollDelta;
        public override bool mousePresent => true;
        public override bool GetMouseButton(int button) => PointerInput.Held(button);
        public override bool GetMouseButtonDown(int button) => PointerInput.Pressed(button);
        public override bool GetMouseButtonUp(int button) => PointerInput.Released(button);

        /// <summary>Stand up the app's event system if there isn't one, reading through this.</summary>
        public static void EnsureEventSystem() {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>().inputOverride = go.AddComponent<PointerBaseInput>();
        }
    }
}
