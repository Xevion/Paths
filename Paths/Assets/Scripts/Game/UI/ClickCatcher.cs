using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI {
    /// <summary>
    /// Swallows pointer clicks so the press resolves here instead of walking up to a handler further up the
    /// hierarchy. A transparent Image catches the raycast, but with no click handler the input module walks
    /// the press up to the next IPointerClickHandler (the modal scrim's dismiss) - this stops that walk, so
    /// clicks on a modal's empty padding don't read as backdrop clicks.
    /// </summary>
    public class ClickCatcher : MonoBehaviour, IPointerClickHandler {
        public void OnPointerClick(PointerEventData e) { }
    }
}
