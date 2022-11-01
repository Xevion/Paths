using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// Attach to any raycast-target control to give it a hover tooltip. Shows after a short delay through
    /// the shared TooltipLayer, hides on exit.
    /// </summary>
    public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        public string text;
        private Coroutine _pending;

        public void OnPointerEnter(PointerEventData e) {
            if (string.IsNullOrEmpty(text)) return;
            _pending = StartCoroutine(ShowAfterDelay());
        }

        public void OnPointerExit(PointerEventData e) {
            Cancel();
            if (TooltipLayer.Exists) TooltipLayer.Instance.Hide();
        }

        private IEnumerator ShowAfterDelay() {
            yield return new WaitForSecondsRealtime(0.18f);
            TooltipLayer.Instance.Show(text);
        }

        private void Cancel() {
            if (_pending != null) StopCoroutine(_pending);
            _pending = null;
        }

        private void OnDisable() {
            Cancel();
            if (TooltipLayer.Exists) TooltipLayer.Instance.Hide();
        }
    }

    /// <summary>The single floating tooltip bubble, lazily built on its own top-most canvas.</summary>
    public class TooltipLayer : MonoBehaviour {
        private static TooltipLayer _instance;
        public static bool Exists => _instance != null;
        public static TooltipLayer Instance => _instance != null ? _instance : (_instance = Build());

        private RectTransform _bubble;
        private TextMeshProUGUI _label;
        private Canvas _canvas;
        private bool _shown;

        private static TooltipLayer Build() {
            var go = new GameObject("Tooltip Layer");
            DontDestroyOnLoad(go);
            return go.AddComponent<TooltipLayer>().Construct();
        }

        private TooltipLayer Construct() {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 32000; // above TMP's dropdown popup (it sorts itself to 30000)
            _canvas.pixelPerfect = true;
            _canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<UiScale>(); // else it renders full size over a shrunken interface

            _bubble = New("Bubble");
            _bubble.SetParent(transform, false);
            _bubble.anchorMin = _bubble.anchorMax = _bubble.pivot = Vector2.zero;
            var bg = _bubble.gameObject.AddComponent<RoundedRectGraphic>();
            bg.color = Theme.SurfaceRaised;
            bg.cornerRadius = Theme.RadiusSm;
            bg.shadowSize = 14f;
            bg.raycastTarget = false;
            var layout = _bubble.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.childControlWidth = layout.childControlHeight = true;
            var fitter = _bubble.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _label = New("Label").gameObject.AddComponent<TextMeshProUGUI>();
            _label.rectTransform.SetParent(_bubble, false);
            _label.font = Fonts.Regular;
            _label.fontSize = Theme.Caption;
            _label.color = Theme.Text;
            _label.raycastTarget = false;
            _label.enableWordWrapping = false;

            _bubble.gameObject.SetActive(false);
            return this;
        }

        public void Show(string text) {
            _label.text = text;
            _bubble.gameObject.SetActive(true);
            _shown = true;
            // the bubble is placed against its own size, which is still the last tooltip's until the
            // fitter runs, and one frame in the wrong place is a visible jump
            LayoutRebuilder.ForceRebuildLayoutImmediate(_bubble);
            Place();
        }

        public void Hide() {
            _shown = false;
            if (_bubble != null) _bubble.gameObject.SetActive(false);
        }

        private void Update() {
            if (_shown) Place();
        }

        /// <summary>
        /// Pin to the cursor, offset up-right, flipped back inside near the edges. The cursor arrives in
        /// screen pixels while this canvas lays out in scaled units, so the position has to be divided
        /// by the scale it renders at and kept inside the canvas rather than inside the screen. Read as
        /// pixels it drifts by however far the interface scale is from 1, which is nothing in a small
        /// window and most of a control's width in a large one.
        /// </summary>
        private void Place() {
            float scale = _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            Vector2 m = PointerInput.Position / scale;
            Vector2 bounds = ((RectTransform) transform).rect.size;
            Vector2 size = _bubble.rect.size;
            float x = m.x + 16f;
            float y = m.y + 16f;
            if (x + size.x > bounds.x) x = m.x - 16f - size.x;
            if (y + size.y > bounds.y) y = m.y - 16f - size.y;
            // a bubble wider or taller than the window would now hang off the near edge instead
            _bubble.anchoredPosition = new Vector2(Mathf.Max(0f, x), Mathf.Max(0f, y));
        }

        private static RectTransform New(string name) {
            return (RectTransform) new GameObject(name, typeof(RectTransform)).transform;
        }
    }
}
