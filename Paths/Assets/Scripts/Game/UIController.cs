using System;
using Algorithms;
using Game.UI;
using LevelGeneration;
using TMPro;
#if UNITY_EDITOR
using UnityEditor; // only Handles, in OnDrawGizmos - breaks player builds otherwise
#endif
using UnityEngine;
using UnityEngine.EventSystems;


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
    Paused,
    Started,
    Reloading
}

/// <summary>
/// Ties the pieces together each frame: feeds input to the GridEditor, runs the play/pause state
/// machine, and drives the PathSolver + Playback into the GridController for rendering. Builds the
/// AppShell UI in code and binds its controls to the solver/playback; still holds the scene refs.
/// </summary>
public class UIController : MonoBehaviour {
    // UI & important App references - serialized but private now, nothing outside this class reaches
    // into them. still wired in the scene exactly as before (the field names didn't change).
    // progressSlider is the legacy bottom slider - retired (hidden) now the toolbar owns the timeline.
    [SerializeField] private CustomSlider progressSlider;
    [SerializeField] private GridController gridController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TextMeshPro debugText;
    [SerializeField] private GameObject gridObject;

    // Animation State
    private AnimationState _animationState;
    private AnimationState _previousAnimationState;
    // where to land after a Reloading recompute - keep playing if we were playing, stay parked if paused
    private AnimationState _resumeState = AnimationState.Started;

    // Grid State & Pathfinding (the grid + start/end + algorithm live in PathSolver now)
    private PathSolver _solver;
    private GridEditor _gridEditor;

    // Playback (timeline scrubbing lives in Playback now)
    [SerializeField] private float clampIncrement;
    [SerializeField] private float speed;
    private Playback _playback;

    // the search layer (seen/expanded/path) is always dimmed a touch so the walls/start/end read
    // clearly on top of it, and dimmed harder while you're editing so the change you're drawing
    // stands out against the still-animating search. structural cells never fade (handled in-shader).
    [SerializeField] private float idleFade = 0.7f;
    [SerializeField] private float editFade = 0.32f;
    [SerializeField] private float fadeSpeed = 6f;
    private float _gridFade = 0.7f;

    // the code-built toolbar + sidebar UI; owns the timeline scrubber and the stat readout
    private AppShell _shell;

    // camera framing + zoom/pan
    private CameraRig _cameraRig;

    // the live grid palette, so the drag ghost can borrow the start/end marker colors
    private GridTheme _gridTheme = GridTheme.Dark();

    // current algorithm selection - kept here so it survives a grid resize (which rebuilds the solver)
    private Algorithm _algorithm = Algorithm.AStar;
    private Heuristic _heuristic = Heuristic.Manhattan;
    private bool _diagonal;

    // grid generation - a seed + a generator + wall density; the sidebar's Grid section drives these,
    // and they're kept here so a resize/regen rebuilds the same layout from the same seed.
    private int _seed = 1;
    private Generator _generator = Generator.Random;
    private float _density = 0.25f;
    private float _weight = 1f; // A* heuristic weight

    // playback pace - a 0..1 speed scale, an optional per-step delay (ms), and loop-on-finish
    private float _speedScale = 0.5f;
    private float _stepDelay = 30f;
    private bool _loop;
    private bool _animateSearch = true; // Display toggle - off snaps each step instead of easing it in

    // grid resize bounds + how much -/= changes it each press
    private const int MinGrid = 4;
    private const int MaxGrid = 200;
    private const int GridStep = 2;

    private void Start() {
        _cameraRig = new CameraRig(mainCamera); // before InitGrid - Resize() frames through it
        InitGrid(gridController.width, gridController.height);

        ApplyGridTheme(0); // Dark; the scene's clear color is an off-theme gray we override here
        _gridFade = idleFade;
        gridController.SetFade(_gridFade);

        // the old bottom slider is retired - the toolbar owns the timeline now. hide the scene object.
        if (progressSlider != null) progressSlider.gameObject.SetActive(false);
        if (debugText != null) debugText.enabled = false;

        BuildShell();
    }

    /// <summary>Build the toolbar + sidebar UI and wire its controls into the solver/playback.</summary>
    private void BuildShell() {
        var go = new GameObject("App Shell");
        _shell = go.AddComponent<AppShell>();
        _shell.initialWidth = gridController.width;
        _shell.initialHeight = gridController.height;
        _shell.initialSeed = _seed;
        _shell.initialDiagonal = _diagonal;
        _shell.onPlayPause = TogglePlayPause;
        _shell.onScrub = fraction => _playback.SeekFraction(fraction);
        _shell.onAlgorithm = OnAlgorithm;
        _shell.onHeuristic = OnHeuristic;
        _shell.onDiagonal = OnDiagonal;
        _shell.onWeight = w => { _weight = w; _solver.Weight = w; Resolve(); };
        _shell.onGenerate = () => RegenGrid(gridController.width, gridController.height);
        _shell.onWidth = w => RegenGrid(w, gridController.height);
        _shell.onHeight = h => RegenGrid(gridController.width, h);
        _shell.onSeed = s => { _seed = s; RegenGrid(gridController.width, gridController.height); };
        _shell.onGenerator = g => { _generator = g; RegenGrid(gridController.width, gridController.height); };
        _shell.onDensity = d => { _density = d; RegenGrid(gridController.width, gridController.height); };
        _shell.onSpeed = ApplySpeed;
        _shell.onStepDelay = ms => { _stepDelay = ms; _playback.StepDelay = ms; };
        _shell.onLoop = on => _loop = on;
        _shell.onGridLines = on => gridController.SetGridLines(on);
        _shell.onAnimate = on => { _animateSearch = on; ApplySpeed(_speedScale); };
        _shell.onTheme = ApplyGridTheme;
        _shell.Build();
        _shell.SetLegend(_gridTheme);
        _shell.SetPlaying(false);
    }

    /// <summary>
    /// (Re)build the solver, editor and playback for a grid size and reframe the camera. Used at
    /// startup and on resize. Leaves us Stopped on a fresh grid (new walls, new start/end).
    /// </summary>
    private void InitGrid(int width, int height) {
        _solver = new PathSolver(width, height, _seed, _generator, _density);
        _solver.Algorithm = _algorithm; // a fresh solver defaults to A* - re-apply the current pick
        _solver.Heuristic = _heuristic;
        _solver.Diagonal = _diagonal;
        _solver.Weight = _weight;
        _gridEditor = new GridEditor(mainCamera, gridController, _solver);
        _playback = new Playback(speed, clampIncrement);
        ApplySpeed(_speedScale);
        _playback.StepDelay = _stepDelay;
        _animationState = AnimationState.Stopped;
        _previousAnimationState = _animationState;
        _shell?.SetPlayhead(0f);
        Resize();
    }

    /// <summary>Rebuild the grid at a (clamped) size and regenerate it from the current seed/generator.</summary>
    private void RegenGrid(int width, int height) {
        width = Mathf.Clamp(width, MinGrid, MaxGrid);
        height = Mathf.Clamp(height, MinGrid, MaxGrid);
        gridController.Rebuild(width, height);
        InitGrid(width, height);
        _shell?.SetPlaying(_animationState == AnimationState.Started);
    }

    // map the sidebar's 0..1 speed scale to steps/sec, quadratic so the slow end has finer control
    private static float SpeedFor(float scale) => Mathf.Lerp(2f, 250f, scale * scale);

    // playback speed also sets how long a cell transition eases - fast sweeps get snappier pops
    // so consecutive steps don't smear into one blur
    private void ApplySpeed(float scale) {
        _speedScale = scale;
        float speed = SpeedFor(scale);
        _playback.Speed = speed;
        // animate off collapses the transition so cells snap between states
        gridController.SetTransitionTime(_animateSearch ? Mathf.Clamp(8f / speed, 0.06f, 0.35f) : 0f);
    }

    private void Update() {
        _cameraRig.Update(); // zoom/pan first so edits this frame use the moved camera
        UpdateHover();

        AnimationState before = _animationState;
        _animationState = _gridEditor.Process(_animationState);
        // an edit just kicked us into Reloading - remember whether to resume playing or stay parked
        if (_animationState == AnimationState.Reloading && before != AnimationState.Reloading)
            _resumeState = before == AnimationState.Paused ? AnimationState.Paused : AnimationState.Started;

        UpdateGhost();

        // Space and the on-screen Play/Pause button both run through here
        if (Input.GetKeyDown(KeyCode.Space))
            TogglePlayPause();
        // - / = resize the grid (= is the unshifted +, don't care about the shift)
        if (Input.GetKeyDown(KeyCode.Equals))
            RegenGrid(gridController.width + GridStep, gridController.height + GridStep);
        if (Input.GetKeyDown(KeyCode.Minus))
            RegenGrid(gridController.width - GridStep, gridController.height - GridStep);

        switch (_animationState) {
            case AnimationState.Reloading:
                // keep replaying live while you draw - recompute the moment an edit lands and
                // render the pinned step, rather than blanking to the bare grid until mouse-up.
                if (_gridEditor.Dirtied || _playback.State == null)
                    GeneratePath();
                LoadNextState(false); // a reload relays the whole search - snap, don't replay every cell's pop
                _shell.SetPlayhead(_playback.Fraction);

                // mouse up: settle back to playing or parked, whichever we were before the edit
                if (!Input.GetMouseButton(0))
                    _animationState = _resumeState;
                break;
            case AnimationState.Started:
                // don't auto-advance while the user is dragging the timeline scrubber
                if (!_shell.Scrubbing)
                    _playback.Advance(Time.deltaTime);

                _shell.SetPlayhead(_playback.Fraction);

                if (_playback.HasFramesLeft)
                    LoadNextState(!_shell.Scrubbing); // dragging the timeline is a seek, not playback
                else if (_loop)
                    _playback.Rewind(); // hit the end with loop on - replay from the start
                break;
            case AnimationState.Stopped:
                // Render editable grid when fully stopped
                gridController.LoadGridState(_solver.RenderEditable());
                break;
            case AnimationState.Paused:
                if (_playback.HasFramesLeft)
                    LoadNextState(!_shell.Scrubbing);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (_animationState != _previousAnimationState) {
            _previousAnimationState = _animationState;
            _shell.SetPlaying(_animationState == AnimationState.Started); // refresh the transport icon
        }

        // ease the search-layer dim between the idle and editing levels
        float fadeTarget = _animationState == AnimationState.Reloading ? editFade : idleFade;
        _gridFade = Mathf.MoveTowards(_gridFade, fadeTarget, fadeSpeed * Time.deltaTime);
        gridController.SetFade(_gridFade);
    }

    /// <summary>
    /// Play/pause toggle shared by the Spacebar and the toolbar button. Off a stopped grid this kicks
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

    // highlight the cell under the cursor, previewing the wall the brush would draw on an empty
    // one. off while the pointer's over the toolbar/sidebar (that's a UI click, not a grid one).
    private void UpdateHover() {
        Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (!overUi && gridController.CellAt(world, out Vector2Int cell)) {
            bool empty = _solver.GetNode(cell).Walkable && cell != _solver.Start && cell != _solver.End;
            gridController.SetHover(cell, empty);
        } else {
            gridController.ClearHover();
        }
    }

    // grid-only theming - the palette and camera backdrop swap, the UI panels stay on the dark tokens
    private void ApplyGridTheme(int index) {
        _gridTheme = GridTheme.Preset(index);
        gridController.ApplyTheme(_gridTheme);
        mainCamera.backgroundColor = _gridTheme.backdrop;
        _shell?.SetLegend(_gridTheme);
    }

    // a token that follows the cursor while a marker is dragged - its own color as a filled disc
    // where it can drop, an orange ring where it can't (over a wall or the other marker).
    private static readonly Color GhostInvalid = new Color(0.98f, 0.45f, 0.12f);

    private void UpdateGhost() {
        if (!_gridEditor.Ghosting) {
            gridController.ClearGhost();
            return;
        }
        Color marker = _gridEditor.GhostKind == ClickType.Start ? _gridTheme.start : _gridTheme.end;
        Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        bool valid = _gridEditor.GhostValid;
        gridController.SetGhost(world, valid ? marker : GhostInvalid, !valid);
    }

    private void OnAlgorithm(Algorithm algorithm) {
        _algorithm = algorithm;
        _solver.Algorithm = algorithm;
        Resolve();
    }

    private void OnHeuristic(Heuristic heuristic) {
        _heuristic = heuristic;
        _solver.Heuristic = heuristic;
        Resolve();
    }

    private void OnDiagonal(bool diagonal) {
        _diagonal = diagonal;
        _solver.Diagonal = diagonal;
        Resolve();
    }

    /// <summary>
    /// Re-run the search after a setting change, keeping the playhead pinned so you can watch the
    /// same step under a different algorithm. No-op if nothing's been solved yet (applies on play).
    /// </summary>
    private void Resolve() {
        if (_playback.State == null)
            return;
        GeneratePath();
        LoadNextState(false); // swapping algorithm re-lays the whole search at the pinned step
    }

    private void GeneratePath() {
        _playback.Load(_solver.Solve());
    }

    private void LoadNextState(bool animate = true) {
        _playback.SyncState();
        ChangeController state = _playback.State;
        gridController.LoadDirtyGridState(state.Current, state.DirtyFlags, animate);
        FeedStats(state);
    }

    // count the search's live footprint straight off the rendered grid: closed cells (expanded +
    // the path drawn over them) and the open frontier (seen-but-not-yet-expanded), plus the path
    // length and the recorded search time up to this step.
    private void FeedStats(ChangeController state) {
        GridNodeType[,] cells = state.Current;
        int width = cells.GetLength(0), height = cells.GetLength(1);
        int expanded = 0, frontier = 0;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                switch (cells[x, y]) {
                    case GridNodeType.Expanded:
                    case GridNodeType.Path:
                        expanded++;
                        break;
                    case GridNodeType.Seen:
                        frontier++;
                        break;
                }

        int pathLength = _solver.Path?.Count ?? 0;
        _shell.SetStats(expanded, frontier, pathLength, state.CurrentRuntime);
    }

    public void OnDrawGizmos() {
        if (!Application.isPlaying) return;

        Vector3 mouse = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3 localScale = gridObject.transform.localScale;
        bool onGrid = gridController.CellAt(mouse, out Vector2Int gridPosition);

        var style = new GUIStyle();
        style.normal.textColor = Color.blue;
        Gizmos.color = Color.blue;

        Gizmos.DrawWireCube(gridController.GetWorldPosition(gridPosition), localScale / (Vector2) gridController.Size);
        #if UNITY_EDITOR
        Handles.Label(mouse, String.Format("{0}{1}",
            gridPosition,
            onGrid && _playback.State != null
                ? $"\n{_playback.State.Current[gridPosition.x, gridPosition.y]}"
                : ""
        ), style);
        #endif
    }

    /// <summary>
    /// Size the grid quad to one world unit per cell and frame the camera on it. The camera does the
    /// fitting now (so it can be zoomed/panned), instead of stretching the quad to fill the screen.
    /// </summary>
    public void Resize() {
        var size = new Vector2(gridController.width, gridController.height);
        gridObject.transform.localScale = new Vector3(size.x, size.y, 0.001f);
        _cameraRig.Frame(gridObject.transform.position, size);
    }
}
