using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// A hover tint that, unlike a Selectable's ColorTint, doesn't mirror its children's hover state.
    /// Unity sends pointer enter/exit to every ancestor, so a dropdown whose popup is its child lights up
    /// whenever a row is hovered; this drives the colour itself and lets a predicate force the "open" look
    /// so the control reads as active (not flickering with the row under the cursor).
    /// </summary>
    public class ControlHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public Graphic target;
        public Color normal;
        public Color hover;
        public Func<bool> active; // forced-on (e.g. dropdown expanded)

        private bool _inside;

        public void OnPointerEnter(PointerEventData e) => _inside = true;
        public void OnPointerExit(PointerEventData e) => _inside = false;

        private void LateUpdate() {
            bool on = _inside || (active != null && active());
            target.color = Color.Lerp(target.color, on ? hover : normal, 0.25f);
        }
    }
}
