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

    public static GridTheme Dark() {
        // search states are accent tints over the empty tile, so the sweep reads as a glow on the
        // board rather than a separate layer of saturated cells
        Color empty = Color.Lerp(Theme.Window, Theme.Surface, 0.22f);
        return new GridTheme {
            backdrop = Theme.Window,
            line = Theme.Window,
            empty = empty,
            wall = Color.Lerp(Theme.Window, Theme.Border, 0.7f),
            start = new Color(0.45f, 0.82f, 0.5f),
            end = Theme.Danger,
            seen = Color.Lerp(empty, Theme.Accent, 0.34f),
            expanded = Color.Lerp(empty, Theme.Accent, 0.14f),
            path = Theme.Accent,
            hover = Theme.Accent,
        };
    }
}
