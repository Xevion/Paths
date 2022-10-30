using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// One scale for every canvas in the app. It follows the window the way CanvasScaler's
    /// ScaleWithScreenSize does, but stops shrinking at a floor: past that the interface keeps its size
    /// and overflows instead, which is what the scrolling and the adaptive rows are for. Add one beside
    /// each CanvasScaler so a dialog can't open at a different size than the toolbar behind it.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class UiScale : MonoBehaviour {
        public const float Floor = 0.75f;

        private static readonly Vector2 Reference = new Vector2(1280f, 800f);
        private const float Match = 0.5f; // the geometric mean of both axes, as ScaleWithScreenSize does

        /// <summary>The interface-scale preference, multiplied on top of the window's own factor.</summary>
        public static float User = 1f;

        public static float Current { get; private set; } = 1f;

        private CanvasScaler _scaler;

        public static float For(float width, float height) {
            float factor = Mathf.Pow(width / Reference.x, 1f - Match) * Mathf.Pow(height / Reference.y, Match);
            return Mathf.Max(Floor, factor) * User;
        }

        private void OnEnable() {
            _scaler = GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            Apply();
        }

        private void Update() => Apply();

        private void Apply() {
            Current = For(Screen.width, Screen.height);
            if (!Mathf.Approximately(_scaler.scaleFactor, Current))
                _scaler.scaleFactor = Current;
        }
    }
}
