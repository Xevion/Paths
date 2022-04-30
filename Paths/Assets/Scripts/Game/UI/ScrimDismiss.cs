using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI {
    /// <summary>
    /// Closes a modal when its dim backdrop is clicked. Fires only when the click both starts and ends on the
    /// scrim itself - the window blocks its own rect, so clicks inside the dialog never reach here.
    /// </summary>
    public class ScrimDismiss : MonoBehaviour, IPointerClickHandler {
        public Action onDismiss;

        public void OnPointerClick(PointerEventData e) {
            if (e.pointerPress == gameObject) onDismiss?.Invoke();
        }
    }
}
