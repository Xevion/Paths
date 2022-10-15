using System;
using Game;
using Paths.Core.Search;
using UnityEngine;
using Terrain = Paths.Core.Search.Terrain;

/// <summary>
/// Handles mouse editing of the grid - drawing/erasing walls and dragging the start/end nodes
/// around - and works out when that edit should kick the animation back into Reloading/Stopped.
/// Pulled out of UIController's Update so all the click/drag mess lives in one place.
/// </summary>
public class GridEditor {
    private readonly Camera _camera;
    private readonly GridController _gridController;
    private readonly PathSolver _solver;

    private ClickType _modify;
    private Vector2Int _lastClickLocation;

    // whether the press in progress started on a cell. a press that began anywhere else, off the
    // grid or on the UI, never becomes an edit no matter where it is dragged afterwards
    private bool _editing;

    // what the left button draws. terrain paints the selected tier onto walkable cells; clicking a
    // cell that is already that tier clears it back to plain, the same toggle the wall brush uses.
    public BrushKind Brush { get; set; } = BrushKind.Wall;
    public Terrain Tier { get; set; } = Terrain.Mud;

    // the tier a terrain drag is laying down, decided by the cell the drag started on
    private Terrain _painting;

    // true on any frame this edited the grid (wall toggle / start / end move), so UIController
    // knows to recompute the path live mid-drag instead of waiting for the mouse to come up
    public bool Dirtied { get; private set; }

    // set alongside Dirtied when the edit was a terrain change, so UIController re-uploads the
    // terrain texture only when it actually moved
    public bool TerrainDirtied { get; private set; }

    // while a start/end marker is being dragged: the cell under the cursor and whether the marker
    // could actually drop there (walkable, not the other marker). UIController draws a ghost from it.
    public bool Ghosting { get; private set; }
    public bool GhostValid { get; private set; }
    public ClickType GhostKind { get; private set; }

    public GridEditor(Camera camera, GridController gridController, PathSolver solver) {
        _camera = camera;
        _gridController = gridController;
        _solver = solver;
    }

    /// <summary>
    /// Process this frame's mouse input. Returns the (possibly changed) animation state - editing
    /// while running bumps it to Reloading, toggling a wall while paused drops it to Stopped.
    /// </summary>
    public AnimationState Process(AnimationState state) {
        Dirtied = false;
        TerrainDirtied = false;
        Ghosting = false;
        if (!PointerInput.Held()) {
            _editing = false;
            return state;
        }

        // a press belongs to whatever it landed on. one that started on the toolbar or a scrub field
        // stays theirs for the whole gesture, so a control that drags the cursor can't draw walls.
        if (PointerInput.UiOwns())
            return state;

        Vector3 worldMouse = _camera.ScreenToWorldPoint(PointerInput.Position);
        // the camera can be zoomed/panned so the cursor is easily off the grid - ignore those clicks
        if (!_gridController.CellAt(worldMouse, out Vector2Int position))
            return state;
        Coord cell = position.ToCoord();

        // Initial click, remember what they grabbed
        if (PointerInput.Pressed()) {
            _editing = true;
            if (cell == _solver.Start)
                _modify = ClickType.Start;
            else if (cell == _solver.End)
                _modify = ClickType.End;
            else if (Brush == BrushKind.Terrain) {
                _modify = ClickType.Paint;
                // already this tier means the click is an erase, and the whole drag erases with it
                _painting = _solver.TerrainAt(cell) == Tier ? Terrain.Plain : Tier;
                PaintTerrain(cell);
                if (state == AnimationState.Paused || state == AnimationState.Started)
                    state = AnimationState.Reloading;
            }
            else {
                bool walkable = _solver.IsWalkable(cell);
                _modify = walkable ? ClickType.Add : ClickType.Remove;
                _solver.SetWalkable(cell, !walkable);
                Dirtied = true;
                // either way we recompute; staying parked at the same step is handled by UIController
                if (state == AnimationState.Paused || state == AnimationState.Started)
                    state = AnimationState.Reloading;
            }

            _lastClickLocation = position;
        }
        // still holding the button & dragged onto a new cell
        else if (_editing && _lastClickLocation != position) {
            _lastClickLocation = position;
            switch (_modify) {
                // wall toggling reloads instantly, but only a real start/end move reloads
                case ClickType.Add:
                    _solver.SetWalkable(cell, false);
                    Dirtied = true;
                    if (ShouldReload(state))
                        state = AnimationState.Reloading;
                    break;
                case ClickType.Remove:
                    _solver.SetWalkable(cell, true);
                    Dirtied = true;
                    if (ShouldReload(state))
                        state = AnimationState.Reloading;
                    break;
                case ClickType.Paint:
                    PaintTerrain(cell);
                    if (ShouldReload(state))
                        state = AnimationState.Reloading;
                    break;
                case ClickType.Start:
                    if (_solver.IsWalkable(cell)) {
                        _solver.Start = cell;
                        Dirtied = true;
                        if (ShouldReload(state))
                            state = AnimationState.Reloading;
                    }

                    break;
                case ClickType.End:
                    if (_solver.IsWalkable(cell)) {
                        _solver.End = cell;
                        Dirtied = true;
                        if (ShouldReload(state))
                            state = AnimationState.Reloading;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // marker drag in progress - flag it and whether the cell under the cursor can take the drop
        if (_editing && (_modify == ClickType.Start || _modify == ClickType.End)) {
            Ghosting = true;
            GhostKind = _modify;
            Coord other = _modify == ClickType.Start ? _solver.End : _solver.Start;
            GhostValid = _solver.IsWalkable(cell) && cell != other;
        }

        return state;
    }

    // terrain only means anything on a cell a route could cross, so walls are left alone
    private void PaintTerrain(Coord cell) {
        if (!_solver.IsWalkable(cell) || _solver.TerrainAt(cell) == _painting)
            return;

        _solver.SetTerrain(cell, _painting);
        Dirtied = true;
        TerrainDirtied = true;
    }

    // recompute on a drag-edit whenever there's an active run to update (playing or parked)
    private static bool ShouldReload(AnimationState state) =>
        state == AnimationState.Started || state == AnimationState.Paused;
}
