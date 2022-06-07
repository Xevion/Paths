using System;
using Algorithms;
using LevelGeneration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// The app's UI: a transport toolbar over a collapsible, scrollable right sidebar of controls, laid
    /// over the live pathfinding grid (which shows through the transparent body). Grew out of the desktop
    /// showcase - the controls now drive the real solver/playback through the callbacks UIController binds,
    /// and the stats/scrubber/play state are pushed back in through the Set* methods each frame.
    /// </summary>
    public class AppShell : MonoBehaviour {
        private const float SidebarWidth = 268f;
        private const float Scale = 0.84f; // the whole sidebar reads one notch smaller than the toolbar

        // wired by UIController before Build(). null-safe so a half-bound shell still runs.
        public Action onPlayPause;
        public Action onGenerate;
        public Action<float> onSpeed;
        public Action<float> onScrub;      // 0..1 along the timeline
        public Action<Algorithm> onAlgorithm;
        public Action<Heuristic> onHeuristic;
        public Action<bool> onDiagonal;
        public Action<float> onWeight;
        public Action<int> onWidth, onHeight, onSeed;
        public Action<Generator> onGenerator;
        public Action<float> onDensity;
        public Action<float> onStepDelay;
        public Action<bool> onLoop;
        public Action<bool> onGridLines, onAnimate;
        public Action<int> onTheme;

        // initial values UIController seeds so the controls open reflecting the real state
        public int initialWidth = 32, initialHeight = 24, initialSeed = 1;
        public bool initialDiagonal;

        private TMP_FontAsset _font;
        private PanelSlide _sidebar;
        private GameObject _modal;

        // toolbar bits pushed from UIController each frame
        private Button _play;
        private TextMeshProUGUI _playIcon;
        private Slider _scrubber;
        private TextMeshProUGUI _statExpanded, _statFrontier, _statPath, _statTime;
        private RectTransform _statsRow; // toggled by the Show statistics checkbox
        private ScrubPress _scrubPress;

        // the timeline slider is being dragged - UIController stops auto-advancing so it doesn't fight the drag
        public bool Scrubbing => _scrubPress != null && _scrubPress.pressed;

        public void Build() {
            _font = Fonts.Regular;
            BuildCanvas();

            RectTransform root = New("Root", transform);
            Stretch(root);
            var v = root.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childControlWidth = v.childControlHeight = true;
            // force-expand only the cross axis (width); on the main axis (height) let flexibleHeight decide,
            // else the toolbar and body split the screen equally instead of toolbar=52 / body=rest.
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            Toolbar(root);

            // the body carries no graphic - the live grid renders behind this overlay canvas and shows
            // through. the sidebar overlays its right edge without reflowing the grid area.
            RectTransform body = New("Body", root);
            body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            _sidebar = Sidebar(body);

            new GameObject("Focus Ring").AddComponent<FocusRing>(); // follows the selected control
            gameObject.AddComponent<KeyboardFocus>(); // Tab moves focus so the ring is keyboard-reachable
            KeyboardFocus.DisableArrowNav(gameObject); // Tab navigates; arrows adjust the focused control
        }

        /// <summary>Update the toolbar stat chips from the real search (called each rendered frame).</summary>
        public void SetStats(int expanded, int frontier, int pathLen, double seconds) {
            _statExpanded.text = expanded.ToString("N0");
            _statFrontier.text = frontier.ToString();
            _statPath.text = pathLen > 0 ? pathLen.ToString() : "—";
            _statTime.text = FormatTime(seconds);
        }

        /// <summary>Move the timeline slider without firing onScrub (driven by playback, not the user).</summary>
        public void SetPlayhead(float fraction) {
            if (_scrubber != null) _scrubber.SetValueWithoutNotify(Mathf.Clamp01(fraction));
        }

        /// <summary>Swap the transport button between play and pause glyphs.</summary>
        public void SetPlaying(bool playing) {
            if (_playIcon != null) _playIcon.text = playing ? Icon.Pause : Icon.Play;
        }

        // pick the unit that keeps the number readable, with one decimal: ns -> µs -> ms
        private static string FormatTime(double seconds) {
            double ns = seconds * 1e9;
            if (ns < 1000.0) return ns.ToString("0.0") + " ns";
            if (ns < 1e6) return (ns / 1e3).ToString("0.0") + " µs";
            return (ns / 1e6).ToString("0.0") + " ms";
        }

        private void Toolbar(Transform parent) {
            RectTransform bar = New("Toolbar", parent);
            bar.gameObject.AddComponent<Image>().color = Theme.Surface;
            var le = bar.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = 52f;
            le.flexibleHeight = 0f;
            Separator(bar, bottom: true);

            var h = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(12, 12, 9, 9);
            h.spacing = Theme.Space2;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;

            _play = UIKit.IconButton(bar, Icon.Play, () => onPlayPause?.Invoke(), UIKit.ButtonStyle.Primary);
            _playIcon = _play.GetComponentInChildren<TextMeshProUGUI>();
            UIKit.Button(bar, _font, "Generate", () => onGenerate?.Invoke(), UIKit.ButtonStyle.Secondary, 96f, Icon.Random);

            ToolbarSep(bar);
            Stats(bar);

            // the transport scrubber takes the flexible middle of the bar
            _scrubber = UIKit.Slider(bar, 0f, 1f, 0f, f => onScrub?.Invoke(f));
            _scrubPress = _scrubber.gameObject.AddComponent<ScrubPress>();

            Legend(bar);
            ToolbarSep(bar);

            // speed: just a bolt + the bar (the separate "Speed" word read awkwardly next to the fill)
            RectTransform speed = UIKit.Row(bar, Theme.Space2);
            var speedLe = speed.gameObject.AddComponent<LayoutElement>();
            speedLe.preferredWidth = speedLe.minWidth = 168f;
            speedLe.flexibleWidth = 0f;
            Icon.Make(speed, Icon.Bolt, Theme.Body, Theme.Muted);
            UIKit.ValueSlider(speed, 0f, 1f, 0.5f, x => Mathf.RoundToInt(x * 100f) + "%",
                x => onSpeed?.Invoke(x), height: 22f);

            UIKit.IconButton(bar, Icon.Sliders, () => _sidebar.Toggle());
            UIKit.IconButton(bar, Icon.Cog, OpenModal);
        }

        // the live readout - a row of stacked stat chips fed by SetStats off the real solver
        private void Stats(Transform bar) {
            _statsRow = UIKit.Row(bar, Theme.Space4);
            _statExpanded = StatChip(_statsRow, "Expanded", 58f);
            _statFrontier = StatChip(_statsRow, "Frontier", 38f);
            _statPath = StatChip(_statsRow, "Path", 38f);
            _statTime = StatChip(_statsRow, "Time", 72f);
        }

        // a tiny caption over a mono value; the value carries a min width so its digits don't jiggle the row
        private TextMeshProUGUI StatChip(Transform parent, string label, float valueWidth) {
            RectTransform col = New("Stat", parent);
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 0f;
            v.childAlignment = TextAnchor.MiddleLeft;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = v.childForceExpandHeight = false;
            TextMeshProUGUI cap = UIKit.Label(col, _font, label.ToUpperInvariant(), UIKit.Type.Caption, Theme.Muted);
            cap.fontSize = 9f;
            cap.characterSpacing = 3f;

            RectTransform valRt = New("Value", col);
            var val = valRt.gameObject.AddComponent<TextMeshProUGUI>();
            val.font = Fonts.Mono;
            val.fontSize = 15f;
            val.color = Theme.Text;
            val.text = "0";
            val.raycastTarget = false;
            val.enableWordWrapping = false;
            valRt.gameObject.AddComponent<LayoutElement>().minWidth = valueWidth;
            return val;
        }

        // a colour key matching the grid's cell states
        private void Legend(Transform bar) {
            RectTransform row = UIKit.Row(bar, Theme.Space3);
            LegendSwatch(row, new Color(0.45f, 0.82f, 0.5f), "Start");
            LegendSwatch(row, Theme.Danger, "End");
            LegendSwatch(row, Theme.Accent, "Path");
            LegendSwatch(row, Color.Lerp(Theme.Surface, Theme.Accent, 0.34f), "Frontier");
            LegendSwatch(row, Color.Lerp(Theme.Surface, Theme.Accent, 0.16f), "Seen");
            LegendSwatch(row, Color.Lerp(Theme.Window, Theme.Border, 0.7f), "Wall");
        }

        private void LegendSwatch(Transform parent, Color color, string label) {
            RectTransform row = UIKit.Row(parent, Theme.Space1);
            RectTransform sw = New("Swatch", row);
            var g = sw.gameObject.AddComponent<RoundedRectGraphic>();
            g.color = color;
            g.cornerRadius = 2f;
            g.raycastTarget = false;
            var le = sw.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 10f;
            le.minHeight = le.preferredHeight = 10f;
            UIKit.Label(row, _font, label, UIKit.Type.Caption, Theme.Muted).fontSize = 11f;
        }

        // a 1px vertical rule between toolbar groups
        private void ToolbarSep(Transform parent) {
            RectTransform sep = New("Sep", parent);
            var img = sep.gameObject.AddComponent<Image>();
            img.color = Theme.Border;
            img.raycastTarget = false;
            var le = sep.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 1f;
            le.minHeight = le.preferredHeight = 24f;
            le.flexibleWidth = le.flexibleHeight = 0f;
        }

        private PanelSlide Sidebar(Transform parent) {
            RectTransform side = New("Sidebar", parent);
            side.anchorMin = new Vector2(1f, 0f); // overlay docked to the right edge, full body height
            side.anchorMax = new Vector2(1f, 1f);
            side.pivot = new Vector2(1f, 0.5f);
            side.sizeDelta = new Vector2(SidebarWidth, 0f);
            side.anchoredPosition = Vector2.zero;
            side.gameObject.AddComponent<Image>().color = Theme.Surface; // static chrome, distinct from the grid
            side.gameObject.AddComponent<RectMask2D>(); // clip the scroll overflow + the slide
            var groupCanvas = side.gameObject.AddComponent<CanvasGroup>();
            Separator(side, left: true);

            // uniformly down-scaled content: one localScale shrinks every control + its spacing together
            RectTransform content = New("Content", side);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.localScale = new Vector3(Scale, Scale, 1f);
            content.sizeDelta = new Vector2(SidebarWidth / Scale, 0f);
            var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(16, 16, 16, 18);
            v.spacing = Theme.Space2;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.childAlignment = TextAnchor.UpperLeft;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Populate(content);

            var scroll = side.gameObject.AddComponent<SidebarScroll>();
            scroll.content = content;
            scroll.viewport = side;
            scroll.scale = Scale;
            scroll.speed = 120f;

            var slide = side.gameObject.AddComponent<PanelSlide>();
            slide.panel = side;
            slide.group = groupCanvas;
            slide.closedOffset = SidebarWidth + 8f;
            return slide;
        }

        private void Populate(Transform col) {
            UIKit.SectionHeader(col, _font, "Search", Icon.Search);
            Sub(col, "Algorithm");
            // switching the algorithm shows/hides the heuristic block below, so the layout shift is visible
            RectTransform heuristic = null;
            UIKit.SegmentedExpand(col,
                new[] { "A*", "Dij", "Grd", "BFS", "DFS", "JPS" },
                new[] { "A*", "Dijkstra", "Greedy", "Breadth-first", "Depth-first", "JPS" }, 0,
                i => {
                    onAlgorithm?.Invoke((Algorithm) i);
                    heuristic.gameObject.SetActive(i == 0 || i == 2 || i == 5); // A*, Greedy, JPS use a heuristic
                });
            heuristic = UIKit.Column(col, Theme.Space2);
            Sub(heuristic, "Heuristic");
            UIKit.Segmented(heuristic, new[] { "Manhattan", "Euclidean", "Chebyshev" }, 0,
                i => onHeuristic?.Invoke((Heuristic) i));
            UIKit.ValueSlider(Field(heuristic, "Weight", fill: true,
                    tooltip: "Scales the estimate - above 1x trades optimality for speed"),
                1f, 3f, 1f, x => x.ToString("0.0") + "x", x => onWeight?.Invoke(x));
            UIKit.Switch(Field(col, "Diagonal", tooltip: "Let the search step diagonally between cells"),
                initialDiagonal, b => onDiagonal?.Invoke(b));
            // TODO: bidirectional search isn't built yet - parked, inert until the Core side lands
            UIKit.Switch(Field(col, "Bidirectional", tooltip: "Search from both ends and meet in the middle"),
                false, _ => { });

            UIKit.SectionHeader(col, _font, "Grid", Icon.Grid);
            UIKit.ScrubField(Field(col, "Width", tooltip: "Grid width in cells"), initialWidth, 8, 200, "",
                i => onWidth?.Invoke(i));
            UIKit.ScrubField(Field(col, "Height", tooltip: "Grid height in cells"), initialHeight, 8, 200, "",
                i => onHeight?.Invoke(i));
            UIKit.ScrubField(Field(col, "Seed", tooltip: "Random seed - same seed regenerates the same maze"),
                initialSeed, 0, 999, "", i => onSeed?.Invoke(i));
            UIKit.Dropdown(Field(col, "Generator", tooltip: "How the starting layout is built"), _font,
                new[] { "Empty", "Random" }, 1, i => onGenerator?.Invoke((Generator) i), 150f);
            UIKit.ValueSlider(Field(col, "Density", fill: true, tooltip: "Fraction of cells that become walls"),
                0f, 1f, 0.25f, x => Mathf.RoundToInt(x * 100f) + "%", x => onDensity?.Invoke(x));

            UIKit.SectionHeader(col, _font, "Playback", Icon.Play);
            UIKit.ValueSlider(Field(col, "Speed", fill: true, tooltip: "How fast the search animates"),
                0f, 1f, 0.5f, x => Mathf.RoundToInt(x * 100f) + "%", x => onSpeed?.Invoke(x));
            UIKit.ValueSlider(Field(col, "Step delay", fill: true, tooltip: "Pause between expanded cells"),
                0f, 200f, 30f, x => Mathf.RoundToInt(x) + "ms", x => onStepDelay?.Invoke(x));
            UIKit.Switch(Field(col, "Loop", tooltip: "Restart the search when it finishes"), false,
                b => onLoop?.Invoke(b));

            UIKit.SectionHeader(col, _font, "Display", Icon.Eye);
            UIKit.Checkbox(col, _font, "Grid lines", true, b => onGridLines?.Invoke(b));
            UIKit.Checkbox(col, _font, "Animate search", true, b => onAnimate?.Invoke(b));
            UIKit.Checkbox(col, _font, "Show statistics", true,
                b => { if (_statsRow != null) _statsRow.gameObject.SetActive(b); });
            // TODO: the distance heatmap isn't built yet - parked, inert until the render side lands
            UIKit.Checkbox(col, _font, "Show distances", false, _ => { });
            UIKit.Dropdown(Field(col, "Theme", tooltip: "Color theme for the grid"), _font,
                new[] { "Dark", "Midnight", "Light" }, 0, i => onTheme?.Invoke(i), 150f);
        }

        // a sidebar form row with a narrower label than the modal's wide one
        private RectTransform Field(Transform parent, string label, bool fill = false, string tooltip = null) {
            return UIKit.Field(parent, _font, label, fill, tooltip, labelWidth: 100f);
        }

        // a small standalone caption above a full-width control (e.g. a segmented group)
        private void Sub(Transform parent, string text) {
            RectTransform row = UIKit.Row(parent, Theme.Space2);
            UIKit.Label(row, _font, text, UIKit.Type.Caption, Theme.Muted);
        }

        // a 1px chrome edge so the toolbar / sidebar read as static frames around the grid
        private static void Separator(RectTransform parent, bool bottom = false, bool left = false) {
            RectTransform line = New("Separator", parent);
            line.gameObject.AddComponent<LayoutElement>().ignoreLayout = true; // the toolbar's HLG must skip it
            var img = line.gameObject.AddComponent<Image>();
            img.color = Theme.Border;
            img.raycastTarget = false;
            if (bottom) {
                line.anchorMin = new Vector2(0f, 0f);
                line.anchorMax = new Vector2(1f, 0f);
                line.pivot = new Vector2(0.5f, 0f);
                line.sizeDelta = new Vector2(0f, 1f);
            } else if (left) {
                line.anchorMin = new Vector2(0f, 0f);
                line.anchorMax = new Vector2(0f, 1f);
                line.pivot = new Vector2(0f, 0.5f);
                line.sizeDelta = new Vector2(1f, 0f);
            }
            line.anchoredPosition = Vector2.zero;
        }

        private void OpenModal() {
            if (_modal != null) return;
            _modal = new GameObject("UI Showcase");
            _modal.AddComponent<UIShowcase>();
        }

        private void BuildCanvas() {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // over the grid, below the settings modal (100)
            canvas.pixelPerfect = true;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            if (EventSystem.current == null) {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private static RectTransform New(string name, Transform parent) {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform) go.transform;
        }

        private static void Stretch(RectTransform rt) {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>Tracks whether a control is being pressed, so the timeline knows when it's being dragged.</summary>
    public class ScrubPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
        public bool pressed;
        public void OnPointerDown(PointerEventData e) => pressed = true;
        public void OnPointerUp(PointerEventData e) => pressed = false;
    }
}
