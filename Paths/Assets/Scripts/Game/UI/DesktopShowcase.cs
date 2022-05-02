using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// A standalone showcase of the always-on desktop layout: a transport toolbar plus a collapsible,
    /// scrollable right sidebar of fast controls (segmented pickers, drag-scrub fields, sliders) over a
    /// faux pathfinding grid. The gear opens the settings modal (UIShowcase) for the rarely-touched stuff.
    /// Spawned by ShowcaseLauncher (F3, or -desktop). Not wired into the app yet - a proof of the surface.
    /// </summary>
    public class DesktopShowcase : MonoBehaviour {
        private const float SidebarWidth = 268f;
        private const float Scale = 0.84f; // the whole sidebar reads one notch smaller than the toolbar

        private TMP_FontAsset _font;
        private PanelSlide _sidebar;

        private GameObject _modal;

        private void Start() {
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

            // body has no layout group: the grid fills it and the sidebar overlays the right edge, so the
            // grid scene stays fixed when the sidebar slides in/out (it's not a layout sibling that reflows).
            RectTransform body = New("Body", root);
            body.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            GridBackdrop(body);
            _sidebar = Sidebar(body);

            new GameObject("Focus Ring").AddComponent<FocusRing>(); // follows the selected control
            gameObject.AddComponent<KeyboardFocus>(); // Tab moves focus so the ring is keyboard-reachable
            KeyboardFocus.DisableArrowNav(gameObject); // Tab navigates; arrows adjust the focused control
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

            bool playing = false;
            Button play = UIKit.IconButton(bar, Icon.Play, null, UIKit.ButtonStyle.Primary);
            var playIcon = play.GetComponentInChildren<TextMeshProUGUI>();
            play.onClick.AddListener(() => {
                playing = !playing;
                playIcon.text = playing ? Icon.Pause : Icon.Play;
            });
            UIKit.Button(bar, _font, "Generate", () => { }, UIKit.ButtonStyle.Secondary, 96f, Icon.Random);

            ToolbarSep(bar);
            Stats(bar);
            UIKit.Flexible(bar);
            Legend(bar);
            ToolbarSep(bar);

            // speed: just a bolt + the bar (the separate "Speed" word read awkwardly next to the fill)
            RectTransform speed = UIKit.Row(bar, Theme.Space2);
            var speedLe = speed.gameObject.AddComponent<LayoutElement>();
            speedLe.preferredWidth = speedLe.minWidth = 168f;
            speedLe.flexibleWidth = 0f;
            Icon.Make(speed, Icon.Bolt, Theme.Body, Theme.Muted);
            UIKit.ValueSlider(speed, 0f, 1f, 0.5f, x => Mathf.RoundToInt(x * 100f) + "%", _ => { }, height: 22f);

            UIKit.IconButton(bar, Icon.Sliders, () => _sidebar.Toggle());
            UIKit.IconButton(bar, Icon.Cog, OpenModal);
        }

        // the live readout - a row of stacked stat chips fed by a looping fake search
        private void Stats(Transform bar) {
            RectTransform row = UIKit.Row(bar, Theme.Space4);
            var anim = gameObject.AddComponent<StatsAnimator>();
            anim.expanded = StatChip(row, "Expanded", 58f);
            anim.frontier = StatChip(row, "Frontier", 38f);
            anim.path = StatChip(row, "Path", 38f);
            anim.time = StatChip(row, "Time", 72f);
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

        // a colour key matching the grid's cell states (opaque equivalents of the faux grid's tints)
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

        private void GridBackdrop(Transform parent) {
            RectTransform grid = New("Grid", parent);
            Stretch(grid); // fixed full-body scene, never reflows
            grid.gameObject.AddComponent<Image>().color = Theme.Window; // base behind the cells / grid lines
            RectTransform cells = New("Cells", grid);
            Stretch(cells);
            cells.gameObject.AddComponent<FauxGrid>();
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
                i => heuristic.gameObject.SetActive(i == 0 || i == 2 || i == 5)); // A*, Greedy, JPS use a heuristic
            heuristic = UIKit.Column(col, Theme.Space2);
            Sub(heuristic, "Heuristic");
            UIKit.Segmented(heuristic, new[] { "Manhattan", "Euclidean", "Chebyshev" }, 0, _ => { });
            UIKit.ValueSlider(Field(heuristic, "Weight", fill: true,
                    tooltip: "Scales the estimate - above 1x trades optimality for speed"),
                1f, 3f, 1f, x => x.ToString("0.0") + "x", _ => { });
            UIKit.Switch(Field(col, "Diagonal", tooltip: "Let the search step diagonally between cells"), true, _ => { });
            UIKit.Switch(Field(col, "Bidirectional", tooltip: "Search from both ends and meet in the middle"),
                false, _ => { });

            UIKit.SectionHeader(col, _font, "Grid", Icon.Grid);
            UIKit.ScrubField(Field(col, "Width", tooltip: "Grid width in cells"), 32, 8, 200, "", _ => { });
            UIKit.ScrubField(Field(col, "Height", tooltip: "Grid height in cells"), 24, 8, 200, "", _ => { });
            UIKit.ScrubField(Field(col, "Seed", tooltip: "Random seed - same seed regenerates the same maze"),
                7, 0, 999, "", _ => { });
            UIKit.Dropdown(Field(col, "Generator", tooltip: "How the starting maze is built"), _font,
                new[] { "Empty", "Rooms", "Random", "Maze" }, 1, _ => { }, 150f);
            UIKit.ValueSlider(Field(col, "Density", fill: true, tooltip: "Fraction of cells that become walls"),
                0f, 1f, 0.25f, x => Mathf.RoundToInt(x * 100f) + "%", _ => { });

            UIKit.SectionHeader(col, _font, "Playback", Icon.Play);
            UIKit.ValueSlider(Field(col, "Speed", fill: true, tooltip: "How fast the search animates"),
                0f, 1f, 0.5f, x => Mathf.RoundToInt(x * 100f) + "%", _ => { });
            UIKit.ValueSlider(Field(col, "Step delay", fill: true, tooltip: "Pause between expanded cells"),
                0f, 200f, 30f, x => Mathf.RoundToInt(x) + "ms", _ => { });
            UIKit.Switch(Field(col, "Loop", tooltip: "Restart the search when it finishes"), false, _ => { });

            UIKit.SectionHeader(col, _font, "Display", Icon.Eye);
            UIKit.Checkbox(col, _font, "Grid lines", true, _ => { });
            UIKit.Checkbox(col, _font, "Animate search", true, _ => { });
            UIKit.Checkbox(col, _font, "Show statistics", true, _ => { });
            UIKit.Checkbox(col, _font, "Show distances", false, _ => { });
            UIKit.Dropdown(Field(col, "Theme", tooltip: "Color theme for the grid and UI"), _font,
                new[] { "Dark", "Midnight", "Light" }, 0, _ => { }, 150f);
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
            canvas.sortingOrder = 50; // below the modal (100) so the gear opens it on top
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
}
