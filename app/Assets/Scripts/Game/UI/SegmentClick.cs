using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI {
    /// <summary>
    /// A single segment of a SegmentedControl: forwards a click to the control (which commits + focuses it)
    /// and gives an inactive segment a faint hover wash. Segments aren't Selectables on purpose, so the whole
    /// group reads as one Tab stop rather than one-per-option.
    /// </summary>
    public class SegmentClick : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {
        public SegmentedControl control;
        public int index;
        public RoundedRectGraphic fill;

        public void OnPointerClick(PointerEventData e) => control.Pick(index);

        public void OnPointerEnter(PointerEventData e) {
            if (!control.IsCurrent(index) && control.IsInteractable())
                fill.color = new Color(1f, 1f, 1f, 0.06f);
        }

        public void OnPointerExit(PointerEventData e) {
            if (!control.IsCurrent(index)) fill.color = new Color(1f, 1f, 1f, 0f);
        }
    }
}
