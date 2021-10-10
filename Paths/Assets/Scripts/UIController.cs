using System;
using Algorithms;
using TMPro;
#if UNITY_EDITOR
using UnityEditor; // only Handles, in OnDrawGizmos - breaks player builds otherwise
#endif
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
/// Ties the pieces together each frame: feeds input to the GridEditor, runs the play/pause state
/// machine, and drives the PathSolver + Playback into the GridController for rendering. Still holds
/// all the scene references. Used to do everything itself, slowly being broken apart.
/// </summary>
public class UIController : MonoBehaviour {
    // UI & important App references
    public CustomSlider progressSlider;
    public GridController gridController;
    public Camera mainCamera;
    public TextMeshPro debugText;
    public GameObject gridObject;

    // Animation State
    private AnimationState _animationState;
    private AnimationState _previousAnimationState;

    // Grid State & Pathfinding (the grid + start/end + algorithm live in PathSolver now)
    private PathSolver _solver;
    private GridEditor _gridEditor;

    // Playback (timeline scrubbing lives in Playback now)
    public float clampIncrement;
    public float speed;
    private Playback _playback;

    private void Start() {
        _solver = new PathSolver(gridController.width, gridController.height);
        _gridEditor = new GridEditor(mainCamera, gridController, progressSlider, _solver);
        _animationState = AnimationState.Stopped;
        _previousAnimationState = _animationState;
        _playback = new Playback(speed, clampIncrement);

        Resize();
        progressSlider.onValueChanged.AddListener((value) => MoveToSlider(value));
    }

    private void Update() {
        _animationState = _gridEditor.Process(_animationState);

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
                    if (_playback.AtEnd)
                        _playback.Rewind();
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
                    gridController.LoadGridState(_solver.RenderEditable());

                progressSlider.SetValueWithoutNotify(_playback.Fraction);
                break;
            case AnimationState.Started:
                // Calculate how much to move forward
                if (!progressSlider.IsPressed)
                    _playback.Advance(Time.deltaTime);

                progressSlider.SetValueWithoutNotify(_playback.Fraction);

                if (_playback.HasFramesLeft)
                    LoadNextState();
                break;
            case AnimationState.Stopped:
                // Render editable grid when fully stopped
                gridController.LoadGridState(_solver.RenderEditable());
                break;
            case AnimationState.Paused:
                if (_playback.HasFramesLeft)
                    LoadNextState();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (_animationState != _previousAnimationState) {
            _previousAnimationState = _animationState;
        }
    }

    private void GeneratePath() {
        _playback.Load(_solver.Solve());
    }

    private void LoadNextState() {
        _playback.SyncState();
        ChangeController state = _playback.State;
        gridController.LoadDirtyGridState(state.Current, state.DirtyFlags);

        string pathCount = _solver.Path != null ? $"{_solver.Path.Count}" : "N/A";
        debugText.text = $"{state.CurrentRuntime * 1000.0:F1}ms\n" +
                                 $"{_playback.CurrentIndex:000} / {_playback.Count:000}\n" +
                                 $"Path: {pathCount} tiles";
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
            _playback.State != null && _solver.IsValid(gridPosition)
                ? $"\n{_playback.State.Current[gridPosition.x, gridPosition.y]}"
                : ""
        ), style);
        #endif
    }

    /// <summary>
    /// Update the animation progress to the slider's (new) position.
    /// </summary>
    /// <param name="new">The new position on the slider.</param>
    private void MoveToSlider(float @new) {
        _playback.SeekFraction(@new);
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