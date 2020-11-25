using System;
using System.Collections.Generic;
using Algorithms;
using LevelGeneration;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The primary controller of the entire application, managing state, events and sending commands
/// </summary>
public class Manager : MonoBehaviour {
    private IPathfinding _algorithm;
    private ChangeController _state;
    private int _curIndex;
    private Stack<Node> _path;
    private float _runtime;

    public float speed;
    public float clampIncrement;
    public bool moving = true;

    public Camera mainCamera;
    public GameObject gridObject;
    public GridController gridController;
    public TextMeshPro debugText;
    public Slider progressSlider;
    private float? _moveTo;
    private bool _allowPausing = true;

    private int CurrentIndex {
        get => (int) _runtime;
        set => _runtime = value;
    }

    public void Start() {
        GeneratePath();
        Resize();

        progressSlider.onValueChanged.AddListener((value) => MoveToSlider(value));
    }

    // public void OnDrawGizmos() {
    // float size = (float) (10.0 / gridController.size);
    // Gizmos.DrawWireCube(transform.position, new Vector3(size, size, size));
    // }

    /// <summary>
    /// Returns the current time multiplier, based on the latest change in the path.
    /// </summary>
    /// <returns>A positive non-zero float representing how fast the current frame should be processed.</returns>
    private float CurrentMultiplier() {
        if (_state.Index == -1)
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

    public void Update() {
        // Toggle pause with space
        if (_allowPausing && Input.GetKeyDown(KeyCode.Space)) {
            moving = !moving;
        }

        // Increment index if unpaused and not clicking (implying slider may be interacted with)
        if (moving && !Input.GetMouseButton(0)) {
            var increment = Time.deltaTime * speed * CurrentMultiplier();
            if (clampIncrement > 0)
                increment = Mathf.Clamp(increment, 0, _state.Count * Time.deltaTime / clampIncrement);
            _runtime += increment;
        }

        // Load next state in grid or update text
        if (CurrentIndex < _state.Count)
            LoadNextState();
        else {
            // No new states to load, generate new grid
            GeneratePath();
        }

        // Update progress slider silently
        progressSlider.SetValueWithoutNotify(_runtime / _state.Count);
    }

    /// <summary>
    /// Generates a new grid and runs pathfinding.
    /// </summary>
    private void GeneratePath() {
        CurrentIndex = 0;
        var nodeGrid = new NodeGrid(gridController.width, gridController.height);

        Vector2Int start = nodeGrid.RandomPosition();
        // Vector2Int start = new Vector2Int(30, 30);
        Vector2Int end = nodeGrid.RandomPosition();

        nodeGrid.ApplyGenerator(new RandomPlacement(0.3f, true, true));

        nodeGrid.GetNode(start).Walkable = true;
        nodeGrid.GetNode(end).Walkable = true;

        _algorithm = new AStar(nodeGrid);
        _path = _algorithm.FindPath(start, end);
        _state = _algorithm.ChangeController;
    }

    /// <summary>
    /// Loads the appropriate grid state into the shader via the ChangeController instance.
    /// </summary>
    private void LoadNextState() {
        _state.MoveTo(CurrentIndex);
        gridController.LoadDirtyGridState(_state.Current, _state.DirtyFlags);

        string pathCount = _path != null ? $"{_path.Count}" : "N/A";
        debugText.text = $"{_state.CurrentRuntime * 1000.0:F1}ms\n" +
                         $"{this.CurrentIndex:000} / {_state.Count:000}\n" +
                         $"Path: {pathCount} tiles";
    }

    /// <summary>
    /// Scales the GridController GameObject to fit within the Camera
    /// </summary>
    private void Resize() {
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