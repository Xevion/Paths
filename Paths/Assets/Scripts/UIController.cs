using System;
using System.Collections.Generic;
using Algorithms;
using TMPro;
using UnityEditor;
using UnityEngine;


/// <summary>
/// Denotes what the user clicked and is now modifying.
/// Add/Remove is for wall addition/removal, Start and End is for dragging the Start and End nodes around.
/// </summary>
public enum ClickType {
    Add,
    Remove,
    Start,
    End,
    ProgressBar
}

/// <summary>
/// Denotes the precise state of the animation as the user clicks, stops or starts it.
/// Stopped is a full stop, allowing editing and manipulation. The path will not be shown or generated.
/// Started is fully engaged when the user is not currently editing or manipulating.
/// Reloading occurs when the user manipulates the grid while the program is
/// </summary>
public enum AnimationState {
    Stopped,
    Paused,
    Started,
    Reloading
}

/// <summary>
/// A expansive class that controls all UI interactions including grid modifications, slider movement, tool usage etc.
/// All UI elements are referenced and controlled here.
/// </summary>
public class UIController : MonoBehaviour {
    // UI & important App references
    public CustomSlider progressSlider;
    public GridController gridController;
    public Camera mainCamera;
    public TextMeshPro debugText;
    public GameObject gridObject;

    // Animation State, Click Management
    private Vector2Int _lastClickLocation;
    private ClickType _modify;
    private AnimationState _animationState;
    private AnimationState _previousAnimationState;

    private bool EditShouldReload =>
        _animationState == AnimationState.Started;

    // Grid State & Pathfinding
    private NodeGrid _grid;
    private Vector2Int _start;
    private Vector2Int _end;
    private IPathfinding _algorithm;
    private Stack<Node> _path;
    private ChangeController _state;

    // Animation speed & indexing
    public float clampIncrement;
    private float _runtime;
    public int CurrentIndex => (int) Math.Ceiling(_runtime);
    public float speed;

    private void Start() {
        _grid = new NodeGrid(gridController.width, gridController.height);
        _animationState = AnimationState.Stopped;
        _previousAnimationState = _animationState;
        _start = _grid.RandomPosition();
        _end = _grid.RandomPosition();
        _runtime = 0;

        Resize();
        progressSlider.onValueChanged.AddListener((value) => MoveToSlider(value));
    }

    private void Update() {
        if (Input.GetMouseButton(0)) {
            Vector3 worldMouse = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int position = gridController.GetGridPosition(worldMouse);

            // Initial click, remember what they clicked
            if (Input.GetMouseButtonDown(0)) {
                if (progressSlider.IsPressed)
                    _modify = ClickType.ProgressBar;
                else if (position == _start)
                    _modify = ClickType.Start;
                else if (position == _end)
                    _modify = ClickType.End;
                else {
                    Node node = _grid.GetNode(position);
                    _modify = node.Walkable ? ClickType.Add : ClickType.Remove;
                    node.Walkable = !node.Walkable;
                    if (_animationState == AnimationState.Paused)
                        _animationState = AnimationState.Stopped;
                    else if (_animationState == AnimationState.Started)
                        _animationState = AnimationState.Reloading;
                }

                _lastClickLocation = position;
            }
            else {
                // If still holding down the button & the latest movement is over a new grid
                if (_lastClickLocation != position) {
                    _lastClickLocation = position;
                    if (_grid.IsValid(position)) {
                        Node node = _grid.GetNode(position);
                        switch (_modify) {
                            // regular clicking toggles walls
                            // Note: Wall toggling instantly reloads, but only real start/end node movement reloads.
                            case ClickType.Add:
                                node.Walkable = false;
                                if (EditShouldReload)
                                    _animationState = AnimationState.Reloading;
                                break;
                            case ClickType.Remove:
                                node.Walkable = true;
                                if (EditShouldReload)
                                    _animationState = AnimationState.Reloading;
                                break;
                            case ClickType.Start:
                                if (node.Walkable) {
                                    _start = position;
                                    if (EditShouldReload)
                                        _animationState = AnimationState.Reloading;
                                }

                                break;
                            case ClickType.End:
                                if (node.Walkable) {
                                    _end = position;
                                    if (EditShouldReload)
                                        _animationState = AnimationState.Reloading;
                                }

                                break;
                            case ClickType.ProgressBar:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
        }

        // Handle user start/stopping
        if (Input.GetKeyDown(KeyCode.Space)) {
            switch (_animationState) {
                case AnimationState.Stopped:
                    _animationState = AnimationState.Reloading;
                    break;
                case AnimationState.Paused:
                    _animationState = AnimationState.Started;
                    break;
                case AnimationState.Started:
                    // Restart if already on final frame, else simply pause
                    if (CurrentIndex >= _state.Count)
                        _runtime = 0;
                    else
                        _animationState = AnimationState.Paused;
                    break;
                case AnimationState.Reloading:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        switch (_animationState) {
            case AnimationState.Reloading:
                // Reloading seizes while the mouse button is depressed
                if (!Input.GetMouseButton(0)) {
                    GeneratePath();
                    LoadNextState();
                    _animationState = AnimationState.Started;
                }
                else
                    gridController.LoadGridState(_grid.RenderNodeTypes(_start, _end));

                progressSlider.SetValueWithoutNotify(_runtime / _state.Count);
                break;
            case AnimationState.Started:
                // Calculate how much to move forward
                if (!progressSlider.IsPressed) {
                    var increment = Time.deltaTime * speed * CurrentMultiplier();
                    if (clampIncrement > 0)
                        increment = Mathf.Clamp(increment, 0, _state.Count * Time.deltaTime / clampIncrement);
                    _runtime += increment;
                }

                progressSlider.SetValueWithoutNotify(_runtime / _state.Count);

                if (_runtime < _state.Count)
                    LoadNextState();
                break;
            case AnimationState.Stopped:
                // Render editable grid when fully stopped
                gridController.LoadGridState(_grid.RenderNodeTypes(_start, _end));
                break;
            case AnimationState.Paused:
                if (_runtime < _state.Count)
                    LoadNextState();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (_animationState != _previousAnimationState) {
            Debug.Log($"Animation State {_previousAnimationState} -> {_animationState}");
            _previousAnimationState = _animationState;
        }
    }

    private void GeneratePath() {
        _algorithm?.Cleanup(); // cleanup algorithm's edits to node grid

        _runtime = 0f;
        _algorithm = new AStar(_grid);
        _path = _algorithm.FindPath(_start, _end);
        _state = _algorithm.ChangeController;
    }

    private void LoadNextState() {
        // Move to the new calculated index
        _state.MoveTo(Mathf.Clamp(CurrentIndex, 1, _state.Count - 1)); // use Math.max to ensure both start/end nodes are always rendered
        gridController.LoadDirtyGridState(_state.Current, _state.DirtyFlags);

        string pathCount = _path != null ? $"{_path.Count}" : "N/A";
        debugText.text = $"{_state.CurrentRuntime * 1000.0:F1}ms\n" +
                                 $"{CurrentIndex:000} / {_state.Count:000}\n" +
                                 $"Path: {pathCount} tiles";
    }

    /// <summary>
    /// Returns the current time multiplier, based on the latest change in the path.
    /// </summary>
    /// <returns>A positive non-zero float representing how fast the current frame should be processed.</returns>
    private float CurrentMultiplier() {
        if (_state.CurrentChangeIndex == -1)
            return 1;

        switch (_state.CurrentChange.New) {
            case GridNodeType.Path:
                return 1 / 5f;
            case GridNodeType.Empty:
                break;
            case GridNodeType.Wall:
                break;
            case GridNodeType.Start:
                break;
            case GridNodeType.End:
                break;
            case GridNodeType.Seen:
                break;
            case GridNodeType.Expanded:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return 1;
    }

    public void OnDrawGizmos() {
        if (!Application.isPlaying) return;

        Vector3 mouse = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3 localScale = gridObject.transform.localScale;
        Vector2Int gridPosition = gridController.GetGridPosition(mouse);

        var style = new GUIStyle();
        style.normal.textColor = Color.blue;
        Gizmos.color = Color.blue;

        Gizmos.DrawWireCube(gridController.GetWorldPosition(gridPosition), localScale / (Vector2) gridController.Size);
        #if UNITY_EDITOR
        Handles.Label(mouse, String.Format("{0}{1}",
            gridPosition,
            _algorithm != null && _algorithm.NodeGrid.IsValid(gridPosition)
                ? $"\n{_state.Current[gridPosition.x, gridPosition.y]}"
                : ""
        ), style);
        #endif
    }

    /// <summary>
    /// Update the animation progress to the slider's (new) position.
    /// </summary>
    /// <param name="new">The new position on the slider.</param>
    private void MoveToSlider(float @new) {
        if (_state != null)
            _runtime = @new * _state.Count;
    }
    
    /// <summary>
    /// Scales the GridController GameObject to fit within the Camera
    /// </summary>
    public void Resize() {
        float ratioImage = (float) gridController.width / gridController.height;
        float ratioScreen = mainCamera.aspect;

        var orthographicSize = mainCamera.orthographicSize;
        var image = new Vector2(gridController.width, gridController.height);
        var screen = new Vector2(2 * orthographicSize * mainCamera.aspect, orthographicSize * 2);

        gridObject.transform.localScale = ratioScreen > ratioImage
            ? new Vector3(image.x * screen.y / image.y, screen.y, 0.001f)
            : new Vector3(screen.x, image.y * screen.x / image.x, 0.001f);
    }
}