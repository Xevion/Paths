using System;
using Algorithms;
using UnityEngine;
using UnityEngine.EventSystems;

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

    // true on any frame this edited the grid (wall toggle / start / end move), so UIController
    // knows to recompute the path live mid-drag instead of waiting for the mouse to come up
    public bool Dirtied { get; private set; }

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
        if (!Input.GetMouseButton(0))
            return state;
        // don't draw walls through the UI - the toolbar/sidebar/scrubber eat the click, not the grid under it
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return state;

        Vector3 worldMouse = _camera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int position = _gridController.GetGridPosition(worldMouse);
        // the camera can be zoomed/panned so the cursor is easily off the grid - ignore those clicks
        if (!_solver.IsValid(position))
            return state;

        // Initial click, remember what they grabbed
        if (Input.GetMouseButtonDown(0)) {
            if (position == _solver.Start)
                _modify = ClickType.Start;
            else if (position == _solver.End)
                _modify = ClickType.End;
            else {
                Node node = _solver.GetNode(position);
                _modify = node.Walkable ? ClickType.Add : ClickType.Remove;
                node.Walkable = !node.Walkable;
                Dirtied = true;
                // either way we recompute; staying parked at the same step is handled by UIController
                if (state == AnimationState.Paused || state == AnimationState.Started)
                    state = AnimationState.Reloading;
            }

            _lastClickLocation = position;
        }
        // still holding the button & dragged onto a new cell
        else if (_lastClickLocation != position) {
            _lastClickLocation = position;
            Node node = _solver.GetNode(position);
            switch (_modify) {
                // wall toggling reloads instantly, but only a real start/end move reloads
                case ClickType.Add:
                    node.Walkable = false;
                    Dirtied = true;
                    if (ShouldReload(state))
                        state = AnimationState.Reloading;
                    break;
                case ClickType.Remove:
                    node.Walkable = true;
                    Dirtied = true;
                    if (ShouldReload(state))
                        state = AnimationState.Reloading;
                    break;
                case ClickType.Start:
                    if (node.Walkable) {
                        _solver.Start = position;
                        Dirtied = true;
                        if (ShouldReload(state))
                            state = AnimationState.Reloading;
                    }

                    break;
                case ClickType.End:
                    if (node.Walkable) {
                        _solver.End = position;
                        Dirtied = true;
                        if (ShouldReload(state))
                            state = AnimationState.Reloading;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return state;
    }

    // recompute on a drag-edit whenever there's an active run to update (playing or parked)
    private static bool ShouldReload(AnimationState state) =>
        state == AnimationState.Started || state == AnimationState.Paused;
}
