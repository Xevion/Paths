using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// Lets the mouse wheel nudge a Slider while it's hovered - a notch is a fraction of the range. uGUI's
    /// Slider only responds to drags/clicks, so this fills in scroll-to-tune the way the scrub field has.
    /// </summary>
    public class SliderScroll : MonoBehaviour, IScrollHandler {
        public Slider slider;
        public float step = 0.05f; // fraction of the value range per wheel notch

        public void OnScroll(PointerEventData e) {
            if (e.scrollDelta.y == 0f || !slider.IsInteractable()) return;
            float range = slider.maxValue - slider.minValue;
            slider.value = Mathf.Clamp(slider.value + Mathf.Sign(e.scrollDelta.y) * step * range,
                slider.minValue, slider.maxValue);
        }
    }
}
