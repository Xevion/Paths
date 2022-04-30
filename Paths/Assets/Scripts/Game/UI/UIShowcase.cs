using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// A standalone, throwaway showcase screen: a dense tabbed settings window built entirely from the
    /// UI kit, used to evaluate how far the kit can be pushed. Not part of the app - spawned by
    /// ShowcaseLauncher (F2, or the -showcase launch arg).
    /// </summary>
    public class UIShowcase : MonoBehaviour {
        private TMP_FontAsset _font;
        private RectTransform _content;
        private PopIn _pop;
        private readonly System.Collections.Generic.List<GameObject> _tabs =
            new System.Collections.Generic.List<GameObject>();

        private void Start() {
            _font = Fonts.Regular;

            BuildCanvas();
            RectTransform screen = FullScreen();
            RectTransform window = Window(screen);

            Header(window);
            UIKit.Divider(window);
            // diminished: the sidebar owns algorithm + grid now, so the modal is just the occasional settings
            UIKit.TabBar(window, _font, new[] { "Appearance", "About" }, Select);

            _content = UIKit.Column(window, Theme.Space3);
            _content.gameObject.AddComponent<LayoutElement>().minHeight = 260f;

            _tabs.Add(AppearanceTab());
            _tabs.Add(AboutTab());
            Select(0);

            UIKit.Divider(window);
            Footer(window);

            // reuse the desktop's ring if one's already up (it follows the global selection across canvases);
            // only spawn our own when this modal is the standalone showcase
            if (FindObjectOfType<FocusRing>() == null)
                new GameObject("Focus Ring").AddComponent<FocusRing>();

            var keys = gameObject.AddComponent<KeyboardFocus>();
            keys.modal = true; // trap Tab focus inside the dialog
            keys.root = window;
            KeyboardFocus.DisableArrowNav(gameObject); // Tab navigates; arrows adjust the focused control

            // click the dim backdrop (anywhere off the window) to dismiss
            screen.gameObject.AddComponent<ScrimDismiss>().onDismiss = Close;

            // settle the content-fit window size this frame so the open animation is a clean scale, not a
            // scale layered on top of the first-frame layout resize (which read as a flicker)
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(window);

            // scale + fade as it opens; Close() reverses it before the modal is destroyed
            var group = screen.gameObject.AddComponent<CanvasGroup>();
            _pop = gameObject.AddComponent<PopIn>();
            _pop.scaleTarget = window;
            _pop.group = group;
            _pop.Begin(); // apply the collapsed start state this frame, before the first render (no open flicker)
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        // animate out, then destroy - routed from every dismissing control so the close is never instant
        private void Close() {
            if (_pop != null) _pop.Close(() => Destroy(gameObject));
            else Destroy(gameObject);
        }

        private void Select(int index) {
            for (int i = 0; i < _tabs.Count; i++)
                _tabs[i].SetActive(i == index);
        }

        private void BuildCanvas() {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // above the app's own HUD
            // pixelPerfect snaps verts to the grid every frame, which shimmers a sub-pixel scale animation;
            // TMP is SDF so text stays crisp without it
            canvas.pixelPerfect = false;
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

        // a dim full-screen scrim that blocks the app behind and centres the window
        private RectTransform FullScreen() {
            var go = new GameObject("Scrim", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform) go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var scrim = go.AddComponent<Image>();
            scrim.color = new Color(0.02f, 0.03f, 0.05f, 0.72f);
            return rt;
        }

        private RectTransform Window(Transform parent) {
            RectTransform window = UIKit.Panel(parent, Theme.Surface, Theme.RadiusLg, Theme.Space6, Theme.Space4);
            // a flush transparent catcher swallows every click on the card so empty padding never falls through
            // to the dismiss scrim (the rounded graphic's mesh is grown for its shadow - a plain stretched
            // Image is the dependable raycast surface). First sibling, so real controls still sit on top of it.
            var catcher = new GameObject("Backstop", typeof(RectTransform));
            catcher.transform.SetParent(window, false);
            catcher.transform.SetAsFirstSibling();
            var crt = (RectTransform) catcher.transform;
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            catcher.AddComponent<LayoutElement>().ignoreLayout = true;
            catcher.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            // the transparent Image only catches the raycast; without a click handler the press would still
            // walk up to the scrim's dismiss. ClickCatcher absorbs it so empty-padding clicks stay put.
            catcher.AddComponent<ClickCatcher>();
            window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
            window.anchoredPosition = Vector2.zero;
            // the scrim has no layout group, so a LayoutElement width wouldn't be read - the panel's
            // own ContentSizeFitter only drives height, so the width has to be set on the rect directly.
            window.sizeDelta = new Vector2(760f, window.sizeDelta.y);
            return window;
        }

        private void Header(Transform parent) {
            RectTransform row = UIKit.Row(parent, Theme.Space3);
            row.gameObject.AddComponent<LayoutElement>().minHeight = 38f;
            UIKit.Label(row, _font, "Settings", UIKit.Type.Title);
            Spacer(row);
            UIKit.IconButton(row, Icon.Close, Close);
        }

        private GameObject AppearanceTab() {
            RectTransform col = TabColumn();
            UIKit.SectionHeader(col, _font, "Theme", Icon.Brush);
            UIKit.Dropdown(UIKit.Field(col, _font, "Mode", tooltip: "Color theme for the grid and UI"), _font,
                new[] { "Dark", "Midnight", "Light" }, 0, _ => { });
            Swatches(UIKit.Field(col, _font, "Accent", tooltip: "Highlight color for the path and controls"));
            UIKit.Switch(UIKit.Field(col, _font, "Reduce motion",
                tooltip: "Disable the cell-reveal and panel animations"), false, _ => { });

            UIKit.SectionHeader(col, _font, "Playback", Icon.Play);
            UIKit.ValueSlider(UIKit.Field(col, _font, "Speed", fill: true,
                tooltip: "How fast the search animates"), 0f, 1f, 0.5f,
                v => Mathf.RoundToInt(v * 100f) + "%", _ => { });
            UIKit.Checkbox(col, _font, "Show statistics", true, _ => { });
            return col.gameObject;
        }

        private GameObject AboutTab() {
            RectTransform col = TabColumn();
            UIKit.Label(col, _font, "Paths", UIKit.Type.Title);
            TextMeshProUGUI body = UIKit.Label(col, _font,
                "A pathfinding visualizer built in Unity. Draw walls, drag the endpoints, pick an " +
                "algorithm and watch the search animate cell by cell.", UIKit.Type.Body, Theme.Muted);
            body.enableWordWrapping = true;

            RectTransform actions = UIKit.Row(col, Theme.Space2);
            UIKit.Button(actions, _font, "Open demo", () => { }, UIKit.ButtonStyle.Primary, 96f, Icon.Play);
            UIKit.Button(actions, _font, "GitHub", () => { }, UIKit.ButtonStyle.Secondary, 96f, Icon.Github);
            return col.gameObject;
        }

        private void Footer(Transform parent) {
            RectTransform row = UIKit.Row(parent, Theme.Space2);
            row.gameObject.AddComponent<LayoutElement>().minHeight = 40f;
            Spacer(row);
            UIKit.Button(row, _font, "Cancel", Close, UIKit.ButtonStyle.Secondary);
            UIKit.Button(row, _font, "Apply", Close, UIKit.ButtonStyle.Primary, 96f, Icon.Check);
        }

        private RectTransform TabColumn() {
            RectTransform col = UIKit.Column(_content, Theme.Space2);
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return col;
        }

        private void Swatches(Transform parent) {
            Color[] swatches = { Theme.Accent, new Color(0.55f, 0.78f, 0.35f), new Color(0.9f, 0.5f, 0.3f),
                new Color(0.78f, 0.45f, 0.85f) };
            RectTransform row = UIKit.Row(parent, Theme.Space2);
            foreach (Color color in swatches) {
                var go = new GameObject("Swatch", typeof(RectTransform));
                go.transform.SetParent(row, false);
                var g = go.AddComponent<RoundedRectGraphic>();
                g.color = color;
                g.cornerRadius = Theme.RadiusSm;
                var element = go.AddComponent<LayoutElement>();
                element.minWidth = element.minHeight = 26f;
            }
        }

        private static void Spacer(Transform parent) {
            var go = new GameObject("Spacer", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }
    }
}
