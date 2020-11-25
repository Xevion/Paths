using System;
using Algorithms;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Denotes what the user clicked and is now modifying.
/// Add/Remove is for wall addition/removal, Start and End is for dragging the Start and End nodes around.
/// </summary>
public enum ClickType {
    Add,
    Remove,
    Start,
    End
}

/// <summary>
/// Denotes the precise state of the animation as the user clicks, stops or starts it.
/// Stopped is a full stop, allowing editing and manipulation. The path will not be shown or generated.
/// Started is fully engaged when the user is not currently editing or manipulating.
/// Reloading occurs when the user manipulates the grid while the program is
/// </summary>
public enum AnimationState {
    Stopped,
    Started,
    Reloading
}

/// <summary>
/// A expansive class that controls all UI interactions including grid modifications, slider movement, tool usage etc.
/// All UI elements are referenced and controlled here.
/// </summary>
public class UIController : MonoBehaviour {
    public Slider progressSlider;
    public GridController gridController;
    public Manager manager;

    private Vector2Int _lastClickLocation;
    private ClickType _modify;
    private AnimationState _animating;

    private NodeGrid _grid;
    private Vector2Int _start;
    private Vector2Int _end;

    private void Start() {
        _grid = new NodeGrid(gridController.width, gridController.height);
        _animating = AnimationState.Stopped;
        _start = _grid.RandomPosition();
        _end = _grid.RandomPosition();
    }

    private void Update() {
        if (Input.GetMouseButton(0)) {
            Vector3 worldMouse = manager.mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int position = gridController.GetGridPosition(worldMouse);

            // Initial click, remember what they clicked
            if (Input.GetMouseButtonDown(0)) {
                if (position == _start)
                    _modify = ClickType.Start;
                else if (position == _end)
                    _modify = ClickType.End;
                else {
                    Node node = _grid.GetNode(position);
                    _modify = node.Walkable ? ClickType.Add : ClickType.Remove;
                    node.Walkable = !node.Walkable;
                }

                _lastClickLocation = position;
            }
            else {
                // If still holding down the button & the latest movement is over a new grid
                if (_lastClickLocation != position) {
                    _lastClickLocation = position;
                    Node node = _grid.GetNode(position);
                    switch (_modify) {
                        // regular clicking toggles walls
                        // Note: Wall toggling instantly reloads, but only real start/end node movement reloads.
                        case ClickType.Add:
                            node.Walkable = false;
                            _animating = AnimationState.Reloading;
                            break;
                        case ClickType.Remove:
                            node.Walkable = true;
                            _animating = AnimationState.Reloading;
                            break;
                        case ClickType.Start:
                            if (node.Walkable) {
                                _start = position;
                                if (_animating == AnimationState.Started)
                                    _animating = AnimationState.Reloading;
                            }

                            break;
                        case ClickType.End:
                            if (node.Walkable) {
                                _end = position;
                                if (_animating == AnimationState.Started)
                                    _animating = AnimationState.Reloading;
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        if (_animating == AnimationState.Reloading) {
        }
        else if (_animating == AnimationState.Started) {
        }

        gridController.LoadGridState(_grid.RenderNodeTypes(_start, _end));
    }

    /// <summary>
    /// Generates the path and sets up the UI Controller to begin animation.
    /// </summary>
    private void StartAnimation() {
    }

    /// <summary>
    /// Stops the path animation and readies the UI Controller for grid editing.
    /// </summary>
    private void StopAnimation() {
    }
}