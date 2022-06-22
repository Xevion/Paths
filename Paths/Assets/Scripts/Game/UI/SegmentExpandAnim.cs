using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// Eases the flexible-width weights of a SegmentedExpand so the selected segment grows (and the others
    /// shrink) over a few frames instead of snapping. Each segment clips its own label, so the full name is
    /// revealed as the box widens rather than overflowing. Runs on unscaled time.
    /// </summary>
    public class SegmentExpandAnim : MonoBehaviour {
        public LayoutElement[] elements;
        public int target;
        public float speed = 9f;

        public void Select(int index) => target = index;

        private void Update() {
            bool changed = false;
            for (int i = 0; i < elements.Length; i++) {
                float to = i == target ? 1f : 0f;
                float cur = elements[i].flexibleWidth;
                if (Mathf.Approximately(cur, to)) continue;
                elements[i].flexibleWidth =
                    Motion.Reduced ? to : Mathf.MoveTowards(cur, to, Time.unscaledDeltaTime * speed);
                changed = true;
            }
            if (changed) LayoutRebuilder.MarkLayoutForRebuild((RectTransform) transform);
        }
    }
}
