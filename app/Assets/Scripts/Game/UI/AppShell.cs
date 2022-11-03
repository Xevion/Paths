using System;
using Paths.Core;
using Paths.Core.Search;
using Paths.Core.Generation;
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
        private static readonly Vector2 BaseReference = new Vector2(1280f, 800f);
        private const float ScrubMin = 72f;
        private const float Scale = 0.84f; // the whole sidebar reads one notch smaller than the toolbar

        // wired by UIController before Build(). null-safe so a half-bound shell still runs.
        public Action onPlayPause;
        public Action onGenerate;
        public Action<float> onSpeed;
        public Action<float> onScrub;      // 0..1 along the timeline
        public Action<Algorithm> onAlgorithm;
        public Action<Heuristic> onHeuristic;
        public Action<bool> onDiagonal;
        public Action<int> onBrush; // 0 wall, then the terrain tiers in order
        public Action<bool> onIncremental;
        public Action<float> onWeight;
        public Action<int> onWidth, onHeight, onSeed;
        public Action<Generator> onGenerator;
        public Action<float> onDensity;
        public Action<float> onStepDelay;
        public Action<bool> onLoop;
        public Action<bool> onTileGap, onAnimate, onShowDistances, onBidirectional;
        public Action<int> onTheme;

        // initial values UIController seeds so the controls open reflecting the real state
        public int initialWidth = 32, initialHeight = 24, initialSeed = 1;
        public bool initialDiagonal;

        private TMP_FontAsset _font;
        private PanelSlide _sidebar;
        private GameObject _modal;
        private CanvasScaler _scaler; // the settings dialog's interface-scale slider drives this
        private float _uiScale = 1f; // kept here so reopening the dialog shows the scale actually in use

        // toolbar bits pushed from UIController each frame
        private Button _play;
        private TextMeshProUGUI _playIcon;
        private Slider _scrubber;
        private TextMeshProUGUI _statExpanded, _statFrontier, _statPath, _statTime;
        private RectTransform _statsRow; // toggled by the Show statistics checkbox
        private RoundedRectGraphic[] _legend; // recoloured on theme change
        private RoundedRectGraphic[] _terrainKey;
        private SegmentedControl _brush;
        private TextMeshProUGUI _terrainWarning;
        private ScrubPress _scrubPress;
        private ControlGate[] _gates;

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

            Legend(body);
            _sidebar = Sidebar(body); // after the legend, so an open sidebar draws over it

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
            Generate(bar);

            ToolbarSep(bar);
            Brush(bar);

            ToolbarSep(bar);
            Stats(bar);

            // the transport scrubber takes the flexible middle of the bar, down to a floor - without one
            // it is the slack everything else eats, and a zero-width scrubber can't be grabbed
            _scrubber = UIKit.Slider(bar, 0f, 1f, 0f, f => onScrub?.Invoke(f));
            _scrubber.gameObject.GetComponent<LayoutElement>().minWidth = ScrubMin;
            _scrubPress = _scrubber.gameObject.AddComponent<ScrubPress>();

            ToolbarSep(bar);
            Speed(bar);

            UIKit.IconButton(bar, Icon.Sliders, () => _sidebar.Toggle());
            UIKit.IconButton(bar, Icon.Cog, OpenSettings);

            // the transport and the two trailing buttons never step down; everything between them does
            bar.gameObject.AddComponent<AdaptiveRow>().Collect();
        }

        // full label, then the icon alone - the first thing worth giving up, since Generate is also a
        // sidebar control and the die glyph carries it
        private void Generate(Transform bar) {
            Button button = UIKit.Button(bar, _font, "Generate", () => onGenerate?.Invoke(),
                UIKit.ButtonStyle.Secondary, 96f, Icon.Random);
            TextMeshProUGUI label = button.GetComponentsInChildren<TextMeshProUGUI>()[1];
            var element = button.GetComponent<LayoutElement>();
            float full = element.minWidth;
            UIKit.Adaptive(button.gameObject, 3,
                () => { label.gameObject.SetActive(true); element.minWidth = full; },
                () => { label.gameObject.SetActive(false); element.minWidth = 36f; });
        }

        // bolt + fill, then the fill narrows, then the bolt alone
        private void Speed(Transform bar) {
            RectTransform speed = UIKit.Row(bar, Theme.Space2);
            var element = speed.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 0f;
            Icon.Make(speed, Icon.Bolt, Theme.Body, Theme.Muted);
            Slider fill = UIKit.ValueSlider(speed, 0f, 1f, 0.5f,
                x => Mathf.RoundToInt(x * 100f) + "%", x => onSpeed?.Invoke(x), height: 22f);
            UIKit.Adaptive(speed.gameObject, 1,
                () => { fill.gameObject.SetActive(true); Width(element, 168f); },
                () => { fill.gameObject.SetActive(true); Width(element, 104f); },
                () => { fill.gameObject.SetActive(false); Width(element, 20f); });
        }

        private static void Width(LayoutElement element, float width) {
            element.preferredWidth = element.minWidth = width;
        }

        // what the left button draws. the only control here that changes what a click does, so it
        // sits with the transport rather than in the sidebar where it would be a click away.
        private void Brush(Transform bar) {
            RectTransform row = UIKit.Row(bar, Theme.Space2);
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 0f;
            string[] names = { "Wall", "Road", "Mud", "Water" };
            _brush = UIKit.Segmented(row, names, 0, i => onBrush?.Invoke(i));
            // full names, then initials - a four-way picker is still readable as W/R/M/W next to the legend
            UIKit.Adaptive(row.gameObject, 2,
                () => { Rename(names, false); Width(le, 210f); },
                () => { Rename(names, true); Width(le, 116f); });
        }

        private void Rename(string[] names, bool initials) {
            if (_brush == null) return;
            for (int i = 0; i < _brush.segments.Length && i < names.Length; i++) {
                var label = _brush.segments[i].GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = initials ? names[i].Substring(0, 1) : names[i];
            }
        }

        /// <summary>Catch the picker up to a brush armed by its keyboard shortcut.</summary>
        public void SetBrush(int index) {
            if (_brush != null)
                _brush.Show(index);
        }

        // the live readout - a row of stacked stat chips fed by SetStats off the real solver
        private void Stats(Transform bar) {
            _statsRow = UIKit.Row(bar, Theme.Space4);
            _statExpanded = StatChip(_statsRow, "Expanded", 58f);
            _statFrontier = StatChip(_statsRow, "Frontier", 38f);
            _statPath = StatChip(_statsRow, "Path", 38f);
            _statTime = StatChip(_statsRow, "Time", 72f);
            // captions go first (the numbers still read in order), then the whole readout - it is a
            // readout, not a control, so it is the most willing thing on the bar
            var group = _statsRow.GetComponent<HorizontalLayoutGroup>();
            UIKit.Adaptive(_statsRow.gameObject, 4,
                () => { _statsRow.gameObject.SetActive(true); Captions(true); group.spacing = Theme.Space4; },
                () => { _statsRow.gameObject.SetActive(true); Captions(false); group.spacing = Theme.Space2; },
                () => _statsRow.gameObject.SetActive(false));
        }

        private void Captions(bool shown) {
            foreach (Transform chip in _statsRow)
                chip.GetChild(0).gameObject.SetActive(shown);
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

        // a colour key matching the grid's cell states, recoloured with the grid theme. it floats over
        // the bottom-left of the board rather than sitting in the toolbar, which it was crowding, and
        // stays faint until the cursor reaches it so it reads as reference rather than chrome.
        private void Legend(Transform body) {
            RectTransform panel = UIKit.Panel(body, Theme.Surface, Theme.RadiusMd, Theme.Space3, Theme.Space1,
                fitWidth: true); // hug the labels rather than sit at the default 100px
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 0f);
            panel.anchoredPosition = new Vector2(Theme.Space4, Theme.Space4);
            panel.GetComponent<RoundedRectGraphic>().raycastTarget = true; // so it can feel the cursor

            var fade = panel.gameObject.AddComponent<HoverFade>();
            fade.group = panel.gameObject.AddComponent<CanvasGroup>();
            fade.group.alpha = fade.idle;

            _legend = new[] {
                LegendSwatch(panel, "Start"), LegendSwatch(panel, "End"), LegendSwatch(panel, "Path"),
                LegendSwatch(panel, "Frontier"), LegendSwatch(panel, "Seen"), LegendSwatch(panel, "Wall"),
            };
            _terrainKey = new[] {
                LegendSwatch(panel, "Road   0.5x"),
                LegendSwatch(panel, "Mud    2x"),
                LegendSwatch(panel, "Water  4x"),
            };
        }

        /// <summary>Recolour the legend swatches to the active grid palette.</summary>
        public void SetLegend(GridTheme t) {
            if (_legend == null) return;
            // Frontier is the bright open edge (seen), Seen the dim swept area (expanded)
            Color[] cols = { t.start, t.end, t.path, t.seen, t.expanded, t.wall };
            for (int i = 0; i < _legend.Length; i++)
                _legend[i].color = cols[i];

            if (_terrainKey == null) return;
            Color[] terrain = { t.road, t.mud, t.water };
            for (int i = 0; i < _terrainKey.Length; i++)
                _terrainKey[i].color = terrain[i];
        }

        /// <summary>
        /// Warn when the grid has terrain on it and the chosen search does not price it. Better than
        /// showing a route that looks wrong for no visible reason.
        /// </summary>
        public void SetTerrainWarning(bool show) {
            if (_terrainWarning != null)
                _terrainWarning.gameObject.SetActive(show);
        }

        /// <summary>
        /// Grey the settings this configuration's search does not read, so the sidebar stops offering a
        /// choice that changes nothing. Push it on every settings change, since which ones are live
        /// depends on the rest of them. The algorithm picker is not among them and never greys.
        /// </summary>
        public void SetCapabilities(SolverSettings settings) {
            if (_gates == null)
                return;
            foreach (ControlGate gate in _gates) {
                Override cause = Capabilities.Ignoring(settings, gate.control);
                gate.Set(cause == Override.None, Reason(gate.control, cause));
            }
        }

        private static ControlGate Gate(Component row, Control setting) =>
            row.gameObject.AddComponent<ControlGate>().Bind(setting);

        // what a greyed control says when hovered, in place of what it would otherwise do
        private static string Reason(Control control, Override cause) {
            switch (cause) {
                case Override.Algorithm:
                    return control == Control.Bidirectional
                        ? "Only Dijkstra has a version that runs from both ends."
                        : "Only Dijkstra has a version that repairs its plan.";
                case Override.Incremental:
                    return "A repaired plan runs one wave, so it cannot also run from both ends.";
                case Override.JumpPoint:
                    return "Jump Point is octile-scored and 8-connected by design.";
                case Override.Uninformed:
                    return "This search works without an estimate.";
                case Override.Greedy:
                    return "Greedy orders by the estimate alone, so scaling it reorders nothing.";
                default:
                    return null;
            }
        }

        private RoundedRectGraphic LegendSwatch(Transform parent, string label) {
            RectTransform row = UIKit.Row(parent, Theme.Space1);
            RectTransform sw = New("Swatch", row);
            var g = sw.gameObject.AddComponent<RoundedRectGraphic>();
            g.cornerRadius = 2f;
            g.raycastTarget = false;
            var le = sw.gameObject.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 10f;
            le.minHeight = le.preferredHeight = 10f;
            // full-strength text, not Muted - the panel's own fade is what holds it back
            UIKit.Label(row, _font, label, UIKit.Type.Caption, Theme.Text).fontSize = 11f;
            return g;
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
            RectTransform algorithm = UIKit.Column(col, Theme.Space2);
            algorithm.name = "Algorithm";
            Sub(algorithm, "Algorithm");
            UIKit.SegmentedExpand(algorithm,
                new[] { "A*", "Dij", "Grd", "BFS", "DFS", "JPS" },
                new[] { "A*", "Dijkstra", "Greedy", "Breadth-first", "Depth-first", "JPS" }, 0,
                i => onAlgorithm?.Invoke((Algorithm) i));
            RectTransform heuristic = UIKit.Column(col, Theme.Space2);
            heuristic.name = "Heuristic";
            Sub(heuristic, "Heuristic");
            UIKit.SegmentedExpand(heuristic,
                new[] { "Man", "Euc", "Che", "Oct" },
                new[] { "Manhattan", "Euclidean", "Chebyshev", "Octile" }, 0,
                i => onHeuristic?.Invoke((Heuristic) i));
            RectTransform weight = Field(col, "Weight", fill: true,
                tooltip: "Scales the estimate - above 1x trades optimality for speed");
            UIKit.ValueSlider(weight, 1f, 3f, 1f, x => x.ToString("0.0") + "x", x => onWeight?.Invoke(x));
            RectTransform diagonal = Field(col, "Diagonal",
                tooltip: "Let the search step diagonally between cells");
            UIKit.Switch(diagonal, initialDiagonal, b => onDiagonal?.Invoke(b));
            RectTransform bidirectional = Field(col, "Bidirectional",
                tooltip: "Search from both ends and meet in the middle");
            UIKit.Switch(bidirectional, false, b => onBidirectional?.Invoke(b));
            RectTransform incremental = Field(col, "Incremental",
                tooltip: "D* Lite - repair the plan as you edit instead of searching again");
            UIKit.Switch(incremental, false, b => onIncremental?.Invoke(b));

            // everything the pick can take out of play, gated so one table decides what is on offer.
            // the picker itself is absent on purpose: it is the choice, so it has no gate to close.
            _gates = new[] {
                Gate(heuristic, Control.Heuristic), Gate(weight, Control.Weight),
                Gate(diagonal, Control.Diagonal), Gate(bidirectional, Control.Bidirectional),
                Gate(incremental, Control.Incremental)
            };

            _terrainWarning = UIKit.Label(col, _font,
                "This search ignores terrain cost.", UIKit.Type.Caption, Theme.Warning);
            _terrainWarning.enableWordWrapping = true;
            _terrainWarning.gameObject.SetActive(false);


            UIKit.SectionHeader(col, _font, "Grid", Icon.Grid);
            UIKit.ScrubField(Field(col, "Width", tooltip: "Grid width in cells"), initialWidth, 8, 200, "",
                i => onWidth?.Invoke(i));
            UIKit.ScrubField(Field(col, "Height", tooltip: "Grid height in cells"), initialHeight, 8, 200, "",
                i => onHeight?.Invoke(i));
            UIKit.ScrubField(Field(col, "Seed", tooltip: "Random seed - same seed regenerates the same maze"),
                initialSeed, 0, 999, "", i => onSeed?.Invoke(i));
            UIKit.Dropdown(Field(col, "Generator", tooltip: "How the starting layout is built"), _font,
                new[] { "Empty", "Random", "Rooms", "Walk" }, 1, i => onGenerator?.Invoke((Generator) i), 150f);
            UIKit.ValueSlider(Field(col, "Density", fill: true, tooltip: "Fraction of cells that become walls"),
                0f, 1f, 0.25f, x => Mathf.RoundToInt(x * 100f) + "%", x => onDensity?.Invoke(x));

            // speed lives on the toolbar next to the transport - a second slider here just desynced from it
            UIKit.SectionHeader(col, _font, "Playback", Icon.Play);
            UIKit.ValueSlider(Field(col, "Step delay", fill: true, tooltip: "Pause between expanded cells"),
                0f, 200f, 30f, x => Mathf.RoundToInt(x) + "ms", x => onStepDelay?.Invoke(x));
            UIKit.Switch(Field(col, "Loop", tooltip: "Restart the search when it finishes"), false,
                b => onLoop?.Invoke(b));

            UIKit.SectionHeader(col, _font, "Display", Icon.Eye);
            // named for the gap it opens, not for a line: nothing is drawn, the backdrop shows through
            UIKit.Checkbox(col, _font, "Tile gap", true, b => onTileGap?.Invoke(b));
            UIKit.Checkbox(col, _font, "Animate search", true, b => onAnimate?.Invoke(b));
            UIKit.Checkbox(col, _font, "Show statistics", true,
                b => { if (_statsRow != null) _statsRow.gameObject.SetActive(b); });
            UIKit.Checkbox(col, _font, "Show distances", false, b => onShowDistances?.Invoke(b));
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

        internal void OpenSettings() {
            if (_modal != null) return;
            _modal = new GameObject("Settings");
            var dialog = _modal.AddComponent<SettingsDialog>();
            dialog.initialUiScale = _uiScale;
            dialog.onUiScale = SetUiScale;
        }

        private void SetUiScale(float scale) {
            _uiScale = scale;
            UiScale.User = scale; // every canvas reads it, so the dialog scales with the shell behind it
        }

        private void BuildCanvas() {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // over the grid, below the settings modal (100)
            canvas.pixelPerfect = true;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;

            _scaler = gameObject.AddComponent<CanvasScaler>();
            UiScale.User = _uiScale;
            gameObject.AddComponent<UiScale>();
            gameObject.AddComponent<GraphicRaycaster>();

            PointerBaseInput.EnsureEventSystem();
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
