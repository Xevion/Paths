using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// The settings dialog behind the toolbar's cog: app-level preferences, a shortcut reference and an
    /// about page. Per-run controls (algorithm, grid, playback) belong to the sidebar, not here.
    /// </summary>
    public class SettingsDialog : MonoBehaviour {
        private const string Repo = "https://github.com/Xevion/Paths";

        public Action<float> onUiScale;
        public float initialUiScale = 1f; // so reopening shows the scale in use, not a fresh 100%

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
            UIKit.TabBar(window, _font, new[] { "General", "Shortcuts", "About" }, Select);

            _content = UIKit.Column(window, Theme.Space3);
            _content.gameObject.AddComponent<LayoutElement>().minHeight = 260f;

            _tabs.Add(GeneralTab());
            _tabs.Add(ShortcutsTab());
            _tabs.Add(AboutTab());
            Select(0);

            UIKit.Divider(window);
            Footer(window);

            // reuse the app's ring if one's already up - it follows the global selection across canvases
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

            PointerBaseInput.EnsureEventSystem();
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

        // app-wide preferences only - anything you'd touch per run lives in the sidebar
        private GameObject GeneralTab() {
            RectTransform col = TabColumn();
            UIKit.SectionHeader(col, _font, "Interface", Icon.Brush);
            UIKit.Switch(UIKit.Field(col, _font, "Reduce motion",
                tooltip: "Snap the panel and dialog animations instead of easing them"),
                Motion.Reduced, on => Motion.Reduced = on);
            UIKit.ValueSlider(UIKit.Field(col, _font, "Interface scale", fill: true,
                tooltip: "Size of the toolbar and sidebar"), 0.8f, 1.4f, initialUiScale,
                v => Mathf.RoundToInt(v * 100f) + "%", v => onUiScale?.Invoke(v));
            return col.gameObject;
        }

        private GameObject ShortcutsTab() {
            RectTransform col = TabColumn();
            UIKit.SectionHeader(col, _font, "Grid", Icon.Grid);
            Shortcut(col, "Click + drag", "Draw with the selected brush");
            Shortcut(col, "1 - 4", "Pick the brush: wall, road, mud, water");
            Shortcut(col, "Drag start / end", "Move an endpoint");
            Shortcut(col, "Scroll", "Zoom the view");
            Shortcut(col, "Right-drag", "Pan the view");
            Shortcut(col, "- / =", "Shrink or grow the grid");

            UIKit.SectionHeader(col, _font, "Playback", Icon.Play);
            Shortcut(col, "Space", "Play or pause the search");

            UIKit.SectionHeader(col, _font, "Interface", Icon.Sliders);
            Shortcut(col, "Tab", "Move focus between controls");
            Shortcut(col, "Esc", "Close this dialog");
            return col.gameObject;
        }

        // a mono key chip on the left, what it does on the right
        private void Shortcut(Transform parent, string keys, string description) {
            RectTransform row = UIKit.Row(parent, Theme.Space3);
            row.gameObject.AddComponent<LayoutElement>().minHeight = 26f;

            RectTransform chip = UIKit.Panel(row, Theme.Track, Theme.RadiusSm, Theme.Space2, 0f,
                shadow: false, fit: false);
            var chipLe = chip.gameObject.AddComponent<LayoutElement>();
            chipLe.minWidth = chipLe.preferredWidth = 132f;
            chipLe.minHeight = 22f;
            UIKit.Label(chip, Fonts.Mono, keys, UIKit.Type.Caption);

            UIKit.Label(row, _font, description, UIKit.Type.Body, Theme.Muted);
        }

        private GameObject AboutTab() {
            RectTransform col = TabColumn();
            UIKit.Label(col, _font, "Paths", UIKit.Type.Title);
            TextMeshProUGUI body = UIKit.Label(col, _font,
                "A pathfinding visualizer built in Unity. Draw walls, drag the endpoints, pick an " +
                "algorithm and watch the search animate cell by cell.", UIKit.Type.Body, Theme.Muted);
            body.enableWordWrapping = true;

            RectTransform actions = UIKit.Row(col, Theme.Space2);
            UIKit.Button(actions, _font, "GitHub", () => Application.OpenURL(Repo),
                UIKit.ButtonStyle.Secondary, 96f, Icon.Github);
            return col.gameObject;
        }

        // everything here applies live, so there's nothing to confirm or revert - just a way out
        private void Footer(Transform parent) {
            RectTransform row = UIKit.Row(parent, Theme.Space2);
            row.gameObject.AddComponent<LayoutElement>().minHeight = 40f;
            Spacer(row);
            UIKit.Button(row, _font, "Done", Close, UIKit.ButtonStyle.Primary, 96f, Icon.Check);
        }

        private RectTransform TabColumn() {
            RectTransform col = UIKit.Column(_content, Theme.Space2);
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return col;
        }

        private static void Spacer(Transform parent) {
            var go = new GameObject("Spacer", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }
    }
}
