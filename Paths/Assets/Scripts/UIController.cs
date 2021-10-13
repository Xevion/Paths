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
    // where to land after a Reloading recompute - keep playing if we were playing, stay parked if paused
    private AnimationState _resumeState = AnimationState.Started;

    // Grid State & Pathfinding (the grid + start/end + algorithm live in PathSolver now)
    private PathSolver _solver;
    private GridEditor _gridEditor;

    // Playback (timeline scrubbing lives in Playback now)
    public float clampIncrement;
    public float speed;
    private Playback _playback;

    // the search layer (seen/expanded/path) is always dimmed a touch so the walls/start/end read
    // clearly on top of it, and dimmed harder while you're editing so the change you're drawing
    // stands out against the still-animating search. structural cells never fade (handled in-shader).
    public float idleFade = 0.7f;
    public float editFade = 0.32f;
    public float fadeSpeed = 6f;
    private float _gridFade = 0.7f;

    // on-screen help + play/pause overlay (its own canvas, built in code)
    private HudOverlay _hud;

    private void Start() {
        _solver = new PathSolver(gridController.width, gridController.height);
        _gridEditor = new GridEditor(mainCamera, gridController, progressSlider, _solver);
        _animationState = AnimationState.Stopped;
        _previousAnimationState = _animationState;
        _playback = new Playback(speed, clampIncrement);

        _gridFade = idleFade;
        gridController.SetFade(_gridFade);
        _hud = new HudOverlay();
        _hud.Build(debugText, TogglePlayPause); // copies debugText's font/colour so it matches

        Resize();
        progressSlider.onValueChanged.AddListener((value) => MoveToSlider(value));
    }

    private void Update() {
        AnimationState before = _animationState;
        _animationState = _gridEditor.Process(_animationState);
        // an edit just kicked us into Reloading - remember whether to resume playing or stay parked
        if (_animationState == AnimationState.Reloading && before != AnimationState.Reloading)
            _resumeState = before == AnimationState.Paused ? AnimationState.Paused : AnimationState.Started;

        // Space and the on-screen Play/Pause button both run through here
        if (Input.GetKeyDown(KeyCode.Space))
            TogglePlayPause();
        if (Input.GetKeyDown(KeyCode.H))
            _hud.ToggleHelp();

        switch (_animationState) {
            case AnimationState.Reloading:
                // keep replaying live while you draw - recompute the moment an edit lands and
                // render the pinned step, rather than blanking to the bare grid until mouse-up.
                // the wall's already in the grid Solve() runs on, so the search shows it right away.
                // State == null is the first run off a Stopped grid - nothing to replay yet, solve once
                if (_gridEditor.Dirtied || _playback.State == null)
                    GeneratePath();
                LoadNextState();
                progressSlider.SetValueWithoutNotify(_playback.Fraction);

                // mouse up: settle back to playing or parked, whichever we were before the edit
                if (!Input.GetMouseButton(0))
                    _animationState = _resumeState;
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
            _hud.SetState(_animationState); // refresh the Play/Pause label
        }

        // ease the search-layer dim between the idle and editing levels
        float fadeTarget = _animationState == AnimationState.Reloading ? editFade : idleFade;
        _gridFade = Mathf.MoveTowards(_gridFade, fadeTarget, fadeSpeed * Time.deltaTime);
        gridController.SetFade(_gridFade);
    }

    /// <summary>
    /// Play/pause toggle shared by the Spacebar and the HUD button. Off a stopped grid this kicks
    /// the first solve; on the final frame it rewinds instead of pausing so you can replay.
    /// </summary>
    private void TogglePlayPause() {
        switch (_animationState) {
            case AnimationState.Stopped:
                _animationState = AnimationState.Reloading;
                _resumeState = AnimationState.Started; // first run just plays from the start
                break;
            case AnimationState.Paused:
                _animationState = AnimationState.Started;
                break;
            case AnimationState.Started:
                if (_playback.AtEnd)
                    _playback.Rewind();
                else
                    _animationState = AnimationState.Paused;
                break;
            case AnimationState.Reloading:
                break; // mid-recompute, ignore
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
                                 $"{_playback.DisplayIndex:000} / {_playback.Count:000}\n" +
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