using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI {
    /// <summary>
    /// Keeps a panel faint until the cursor reaches it, then brings it up to full. For overlays that
    /// sit on top of the content and should not compete with it when nobody is looking at them.
    /// </summary>
    public class HoverFade : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public CanvasGroup group;
        public float idle = 0.4f;
        public float hover = 1f;

        private bool _inside;

        public void OnPointerEnter(PointerEventData e) => _inside = true;
        public void OnPointerExit(PointerEventData e) => _inside = false;

        private void LateUpdate() {
            float target = _inside ? hover : idle;
            group.alpha = Motion.Reduced ? target : Mathf.Lerp(group.alpha, target, 0.2f);
        }
    }
}
