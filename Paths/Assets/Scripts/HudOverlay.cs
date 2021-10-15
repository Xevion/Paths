using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The on-screen overlay - key hints (Space was impossible to find), a play/pause button, and a
/// placeholder for the algorithm picker. Built in code on its own screen-space canvas so it scales
/// with the window, and styled off the existing debugText (same font/colour) so it matches the
/// stats readout in the corner. The stats themselves still live on debugText. UIController owns one.
/// </summary>
public class HudOverlay {
    private TextMeshProUGUI _help;
    private TextMeshProUGUI _stats;
    private TextMeshProUGUI _playLabel;
    private bool _showHelp = true;

    private const string HelpText =
        "Paths — A* visualizer\n\n" +
        "Space — play / pause\n" +
        "Click + drag — draw / erase walls\n" +
        "Drag green / red — move start / end\n" +
        "Drag the bar — scrub the search\n" +
        "- / = — shrink / grow the grid\n" +
        "Scroll / right-drag — zoom / pan\n\n" +
        "H — hide";

    /// <summary>Build the canvas + widgets. style is copied for font/colour; onPlayPause fires on the button.</summary>
    public void Build(TMP_Text style, Action onPlayPause) {
        EnsureEventSystem();

        var canvasGo = new GameObject("HUD Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // over the slider's canvas
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 800);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // top-left help block
        _help = MakeText(canvasGo.transform, style, 22, TextAlignmentOptions.TopLeft,
            new Vector2(0, 1), new Vector2(16, -16), new Vector2(380, 260));
        _help.enableWordWrapping = false;
        _help.text = HelpText;

        // stats readout, top-right (used to be a world-space text that drifted when the camera moved)
        _stats = MakeText(canvasGo.transform, style, 22, TextAlignmentOptions.TopRight,
            new Vector2(1, 1), new Vector2(-16, -16), new Vector2(300, 140));

        // algorithm picker placeholder, bottom-left above the button
        MakeText(canvasGo.transform, style, 22, TextAlignmentOptions.BottomLeft,
            new Vector2(0, 0), new Vector2(16, 98), new Vector2(300, 30)).text = "Algorithm: A*";

        // play/pause button, bottom-left clear of the slider
        MakeButton(canvasGo.transform, style, onPlayPause, out _playLabel);
    }

    public void SetState(AnimationState state) {
        if (_playLabel != null)
            _playLabel.text = state == AnimationState.Started ? "Pause" : "Play";
    }

    public void SetStats(string text) {
        if (_stats != null)
            _stats.text = text;
    }

    public void ToggleHelp() {
        _showHelp = !_showHelp;
        if (_help != null)
            _help.text = _showHelp ? HelpText : "H — show help";
    }

    private static TextMeshProUGUI MakeText(Transform parent, TMP_Text style, float size,
        TextAlignmentOptions align, Vector2 anchor, Vector2 anchoredPos, Vector2 sizeDelta) {
        var go = new GameObject("HUD Text");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (style != null) {
            t.font = style.font;
            t.color = style.color;
        }
        t.fontSize = size;
        t.alignment = align;
        t.raycastTarget = false; // text never eats clicks meant for the grid

        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = anchoredPos;
        return t;
    }

    private static void MakeButton(Transform parent, TMP_Text style, Action onClick, out TextMeshProUGUI label) {
        var go = new GameObject("Play Button");
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = new Color(0.12f, 0.12f, 0.12f, 0.85f);

        RectTransform rt = image.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(112, 38);
        rt.anchoredPosition = new Vector2(16, 52);

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick());

        label = MakeText(go.transform, style, 22, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero; // stretch over the button
        lrt.anchorMax = Vector2.one;
        lrt.sizeDelta = Vector2.zero;
        label.text = "Play";
    }

    // there's already one for the slider, but build doesn't cost anything if there isn't
    private static void EnsureEventSystem() {
        if (EventSystem.current != null)
            return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }
}
