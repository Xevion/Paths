using Game.UI;
using UnityEngine;

/// <summary>
/// The grid's palette, one final opaque color per cell state. Seeded from the UI theme tokens so
/// the world grid and the interface read as one surface. Injected into the grid material at
/// runtime via GridController.ApplyTheme - the colors serialized on the .mat asset are just
/// editor-time defaults now.
/// </summary>
public class GridTheme {
    public Color backdrop; // camera clear color - the void the board floats on
    public Color line;     // the gap between tiles - the backdrop showing through
    public Color empty;
    public Color wall;
    public Color start;
    public Color end;
    public Color seen;     // the open frontier - the search's bright active edge
    public Color expanded; // closed cells - the big swept area, kept subtle
    public Color path;
    public Color hover;    // the cursor highlight - lifts the cell under the mouse

    /// <summary>The grid palettes, by dropdown index (Dark / Midnight / Light).</summary>
    public static GridTheme Preset(int index) {
        switch (index) {
            case 1: return Midnight();
            case 2: return Light();
            default: return Dark();
        }
    }

    public static GridTheme Dark() {
        // search states are accent tints over the empty tile, so the sweep reads as a glow on the
        // board rather than a separate layer of saturated cells
        Color empty = Color.Lerp(Theme.Window, Theme.Surface, 0.22f);
        return new GridTheme {
            backdrop = Theme.Window,
            line = Theme.Window,
            empty = empty,
            // lifted toward Muted so the fused wall masses read as solid structure, well clear of
            // the empty tile and the dimmed search underneath them
            wall = Color.Lerp(Theme.Border, Theme.Muted, 0.35f),
            start = new Color(0.45f, 0.82f, 0.5f),
            end = Theme.Danger,
            seen = Color.Lerp(empty, Theme.Accent, 0.34f),
            expanded = Color.Lerp(empty, Theme.Accent, 0.14f),
            path = Theme.Accent,
            hover = Theme.Accent,
        };
    }

    // a deeper, cooler dark with a violet search instead of cyan
    public static GridTheme Midnight() {
        Color empty = C("#141A2E");
        Color accent = C("#8A7CF5");
        return new GridTheme {
            backdrop = C("#0B0E1A"),
            line = C("#0B0E1A"),
            empty = empty,
            wall = C("#3E4A78"),
            start = C("#4FD98A"),
            end = C("#E85D8A"),
            seen = Color.Lerp(empty, accent, 0.42f),
            expanded = Color.Lerp(empty, accent, 0.16f),
            path = accent,
            hover = accent,
        };
    }

    // a light board: pale tiles, dark walls, blue search. grid only - the UI stays dark.
    public static GridTheme Light() {
        Color empty = C("#EEF1F6");
        Color accent = C("#2B7FFF");
        return new GridTheme {
            backdrop = C("#E4E8F0"),
            line = C("#CBD2DE"),
            empty = empty,
            wall = C("#454E5F"),
            start = C("#2E9E5B"),
            end = C("#D6473F"),
            seen = Color.Lerp(empty, accent, 0.5f),
            expanded = Color.Lerp(empty, accent, 0.2f),
            path = accent,
            hover = accent,
        };
    }

    private static Color C(string hex) {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}
