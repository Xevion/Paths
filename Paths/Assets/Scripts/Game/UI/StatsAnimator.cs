using TMPro;
using UnityEngine;

namespace Game.UI {
    /// <summary>
    /// Drives the toolbar's statistics readout with a looping fake search so the numbers read as live in
    /// the showcase: nodes expanded ramps up, the frontier swells then drains, the path resolves near the
    /// end, the clock ticks - then it resets and runs again. On integration these come off the real solver.
    /// </summary>
    public class StatsAnimator : MonoBehaviour {
        public TMP_Text expanded, frontier, path, time;
        public float period = 6f;

        private float _t;

        private void Update() {
            _t += Time.unscaledDeltaTime;
            float p = Mathf.Repeat(_t, period) / period;

            expanded.text = Mathf.RoundToInt(Mathf.SmoothStep(0f, 1864f, p)).ToString("N0");
            frontier.text = Mathf.RoundToInt(Mathf.Sin(p * Mathf.PI) * 118f + 6f).ToString();
            bool solved = p > 0.86f;
            path.text = solved ? "142" : "—";
            time.text = FormatTime(Mathf.SmoothStep(0f, 1f, p) * 0.0473); // 0 .. 47.3 ms over the cycle
        }

        // pick the unit that keeps the number readable, with one decimal: ns -> µs -> ms
        private static string FormatTime(double seconds) {
            double ns = seconds * 1e9;
            if (ns < 1000.0) return ns.ToString("0.0") + " ns";
            if (ns < 1e6) return (ns / 1e3).ToString("0.0") + " µs";
            return (ns / 1e6).ToString("0.0") + " ms";
        }
    }
}
