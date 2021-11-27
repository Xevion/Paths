using System;
using System.Collections.Generic;
using Algorithms;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The on-screen overlay - key hints (Space was impossible to find), a play/pause button, the
/// algorithm/heuristic pickers and the diagonal toggle, plus the stats readout. Built in code on its
/// own screen-space canvas so it scales with the window, and styled off the existing debugText (same
/// font/colour) so it all matches. UIController owns one and wires the controls back to PathSolver.
/// </summary>
public class HudOverlay {
    private Transform _root;
    private TMP_Text _style;
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

        _root = canvasGo.transform;
        _style = style;

        // top-left help block
        _help = MakeText(_root, style, 22, TextAlignmentOptions.TopLeft,
            new Vector2(0, 1), new Vector2(16, -16), new Vector2(380, 260));
        _help.enableWordWrapping = false;
        _help.text = HelpText;

        // stats readout, top-right (used to be a world-space text that drifted when the camera moved)
        _stats = MakeText(_root, style, 22, TextAlignmentOptions.TopRight,
            new Vector2(1, 1), new Vector2(-16, -16), new Vector2(300, 140));

        // play/pause button, bottom-left clear of the slider
        MakeButton(_root, style, onPlayPause, out _playLabel);
    }

    /// <summary>
    /// Add the algorithm + heuristic dropdowns and the diagonal toggle, stacked bottom-left above the
    /// play button. Each one reports the new value back so UIController can re-solve.
    /// </summary>
    public void AddAlgorithmControls(
        Algorithm algorithm, Action<Algorithm> onAlgorithm,
        Heuristic heuristic, Action<Heuristic> onHeuristic,
        bool diagonal, Action<bool> onDiagonal) {
        var algorithms = (Algorithm[]) Enum.GetValues(typeof(Algorithm));
        MakeDropdown(algorithms, algorithm, AlgorithmName, new Vector2(16, 176), onAlgorithm);

        var heuristics = (Heuristic[]) Enum.GetValues(typeof(Heuristic));
        MakeDropdown(heuristics, heuristic, h => h.ToString(), new Vector2(16, 140), onHeuristic);

        MakeToggle("Diagonal", diagonal, new Vector2(18, 112), onDiagonal);
    }

    private static string AlgorithmName(Algorithm algorithm) {
        switch (algorithm) {
            case Algorithm.AStar: return "A*";
            case Algorithm.Dijkstra: return "Dijkstra";
            case Algorithm.Greedy: return "Greedy";
            case Algorithm.BreadthFirst: return "BFS";
            case Algorithm.DepthFirst: return "DFS";
            case Algorithm.JumpPoint: return "JPS";
            default: return algorithm.ToString();
        }
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

    /// <summary>A TMP dropdown over an enum's values, styled to match. Built via TMP's own factory.</summary>
    private void MakeDropdown<T>(T[] values, T current, Func<T, string> label, Vector2 anchoredPos,
        Action<T> onChange) {
        GameObject go = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
        go.transform.SetParent(_root, false);

        var rt = (RectTransform) go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(180, 30);
        rt.anchoredPosition = anchoredPos;

        var dropdown = go.GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        var options = new List<string>(values.Length);
        foreach (T value in values)
            options.Add(label(value));
        dropdown.AddOptions(options);
        dropdown.value = Array.IndexOf(values, current);
        dropdown.RefreshShownValue();
        // set after the value so the initial assignment doesn't fire it
        dropdown.onValueChanged.AddListener(index => onChange(values[index]));

        // match the HUD font (leave the default light dropdown colours so the popup text stays readable)
        if (_style != null) {
            if (dropdown.captionText != null) dropdown.captionText.font = _style.font;
            if (dropdown.itemText != null) dropdown.itemText.font = _style.font;
        }
    }

    /// <summary>A small checkbox + label, dark box / light tick so it reads on the grid.</summary>
    private void MakeToggle(string text, bool on, Vector2 anchoredPos, Action<bool> onChange) {
        var go = new GameObject("Toggle");
        go.transform.SetParent(_root, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(200, 24);
        rt.anchoredPosition = anchoredPos;

        var toggle = go.AddComponent<Toggle>();

        var box = new GameObject("Box").AddComponent<Image>();
        box.transform.SetParent(go.transform, false);
        box.color = new Color(0.12f, 0.12f, 0.12f, 0.85f);
        RectTransform brt = box.rectTransform;
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0, 0.5f);
        brt.sizeDelta = new Vector2(20, 20);
        brt.anchoredPosition = new Vector2(2, 0);

        var check = new GameObject("Check").AddComponent<Image>();
        check.transform.SetParent(box.transform, false);
        check.color = _style != null ? _style.color : Color.white;
        RectTransform crt = check.rectTransform;
        crt.anchorMin = new Vector2(0.18f, 0.18f);
        crt.anchorMax = new Vector2(0.82f, 0.82f);
        crt.sizeDelta = Vector2.zero;

        TextMeshProUGUI label = MakeText(go.transform, _style, 20, TextAlignmentOptions.MidlineLeft,
            new Vector2(0, 0.5f), new Vector2(28, 0), new Vector2(170, 24));
        label.text = text;

        toggle.targetGraphic = box;
        toggle.graphic = check;
        toggle.isOn = on;
        toggle.onValueChanged.AddListener(value => onChange(value));
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
