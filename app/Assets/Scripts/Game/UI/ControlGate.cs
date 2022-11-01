using System.Collections.Generic;
using Paths.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    /// <summary>
    /// Turns one setting's controls on or off as a unit: drained of colour, unclickable, out of the Tab
    /// order, and hovered for what is ignoring it rather than what it does. Closing a gate leaves the
    /// setting where it was, so lifting the override gives the old choice back instead of a default.
    /// </summary>
    public class ControlGate : MonoBehaviour {
        // enough to read as held back, not so little that the row sinks into the panel. losing the
        // colour is what says unavailable here, so this does not have to carry it alone.
        private const float Dim = 0.6f;

        public Control control;

        /// <summary>Whether the configured search reads this setting, as last pushed in.</summary>
        public bool Live { get; private set; } = true;

        private CanvasGroup _group;
        private Tooltip[] _tips;
        private string[] _said; // what each tooltip reads while the gate is open
        private HoverCursor[] _cursors;
        private UISwitch[] _switches;
        private Graphic[] _painted;
        private Color[] _colors; // each drained graphic's open colour

        /// <summary>Take over the subtree already built under this object.</summary>
        public ControlGate Bind(Control setting) {
            control = setting;
            _group = gameObject.AddComponent<CanvasGroup>();
            _tips = GetComponentsInChildren<Tooltip>(true);
            _cursors = GetComponentsInChildren<HoverCursor>(true);
            _switches = GetComponentsInChildren<UISwitch>(true);

            _said = new string[_tips.Length];
            for (int i = 0; i < _tips.Length; i++)
                _said[i] = _tips[i].text;

            // a switch paints its own track and knob from the state it is holding, so draining those
            // here would fight it, and restoring them would put an off colour under an on knob
            var painted = new List<Graphic>();
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
                if (!PaintedByASwitch(graphic))
                    painted.Add(graphic);
            _painted = painted.ToArray();
            _colors = new Color[_painted.Length];
            for (int i = 0; i < _painted.Length; i++)
                _colors[i] = _painted[i].color;
            return this;
        }

        /// <summary>Open the gate, or close it and say why. The reason is only read when closing.</summary>
        public void Set(bool live, string reason) {
            Live = live;
            // raycasts keep coming through, so a closed gate can still be hovered for its reason
            _group.alpha = live ? 1f : Dim;
            _group.interactable = live;
            foreach (HoverCursor cursor in _cursors)
                cursor.enabled = live;
            foreach (UISwitch toggle in _switches)
                toggle.SetAvailable(live);
            for (int i = 0; i < _painted.Length; i++)
                _painted[i].color = live ? _colors[i] : Drained(_colors[i]);
            for (int i = 0; i < _tips.Length; i++)
                _tips[i].text = live ? _said[i] : reason;
        }

        // an unavailable control gives up its colour rather than its contrast: a selection still reads
        // as the selection, in grey, instead of as a faint version of a live one. weighting the
        // channels by how bright each looks keeps a dark fill dark and a light one light.
        private static Color Drained(Color color) {
            float grey = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
            return new Color(grey, grey, grey, color.a);
        }

        private bool PaintedByASwitch(Graphic graphic) {
            foreach (UISwitch toggle in _switches)
                if (toggle.track == graphic || toggle.knobGraphic == graphic)
                    return true;
            return false;
        }
    }
}
