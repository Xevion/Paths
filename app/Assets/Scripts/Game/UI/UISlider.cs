using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// A Slider whose keyboard arrows step finer than uGUI's hardcoded 10%: left/right take a small step,
    /// up/down a coarser one. Tab handles focus, so the arrows are free to mean "adjust this control" in
    /// both axes instead of left/right adjusting while up/down jumps to another field.
    /// </summary>
    public class UISlider : Slider {
        public float fineStep = 0.02f;  // left/right, as a fraction of the range
        public float coarseStep = 0.1f; // up/down

        public override void OnMove(AxisEventData e) {
            if (!IsActive() || !IsInteractable()) { base.OnMove(e); return; }
            float range = maxValue - minValue;
            switch (e.moveDir) {
                case MoveDirection.Left: Step(-fineStep * range); break;
                case MoveDirection.Right: Step(fineStep * range); break;
                case MoveDirection.Up: Step(coarseStep * range); break;
                case MoveDirection.Down: Step(-coarseStep * range); break;
            }
        }

        private void Step(float delta) {
            value = wholeNumbers ? Mathf.Round(value + delta) : value + delta;
        }
    }
}
