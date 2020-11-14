using System;
using System.Collections.Generic;
using Algorithms;
using LevelGeneration;
using TMPro;
using UnityEngine;

/// <summary>
/// The primary controller of the entire application, managing state, events and sending commands
/// </summary>
public class Manager : MonoBehaviour {
    private IPathfinding _algorithm;
    private List<GridState> _states;
    private int _curIndex;
    private Stack<Node> _path;
    private float _lastStart;
    private float _runtime;

    public Camera mainCamera;
    public GameObject gridObject;
    public GridController gridController;
    public TextMeshPro debugText;
    public float speed;
    public float clampIncrement;

    private int CurrentIndex {
        get => (int) _runtime;
        set => _runtime = value;
    }

    public void Start() {
        _states = new List<GridState>();
        GeneratePath();
        Resize();
    }

    // public void OnDrawGizmos() {
    // float size = (float) (10.0 / gridController.size);
    // Gizmos.DrawWireCube(transform.position, new Vector3(size, size, size));
    // }

    public void Update() {
        var increment = Time.deltaTime * speed;
        if (clampIncrement > 0)
            increment = Mathf.Clamp(increment, 0, _states.Count * Time.deltaTime / clampIncrement);
        _runtime += increment;

        if (CurrentIndex < _states.Count)
            this.LoadNextState();
        else {
            try {
                _lastStart = Time.realtimeSinceStartup;
                GeneratePath();
                CurrentIndex = 0;
                // _curIndex = path != null && path.Count > 30 ? 0 : _states.Count;
            }
            catch (ArgumentOutOfRangeException) {
            }
        }
    }

    private void GeneratePath() {
        var nodeGrid = new NodeGrid(gridController.width, gridController.height);
        _algorithm = new AStar(nodeGrid);

        Vector2Int start = nodeGrid.RandomPosition();
        // Vector2Int start = new Vector2Int(30, 30);
        Vector2Int end = nodeGrid.RandomPosition();

        nodeGrid.ApplyGenerator(new RandomPlacement(0.25f, true));

        nodeGrid.GetNode(start).Walkable = true;
        nodeGrid.GetNode(end).Walkable = true;

        _path = _algorithm.FindPath(start, end);

        _states = _algorithm.GetStates();
    }

    private void LoadNextState() {
        GridState state = _states[CurrentIndex];
        gridController.LoadGridState(state);

        float change = state.Time - _lastStart;
        string pathCount = _path != null ? $"{_path.Count}" : "N/A";
        debugText.text = $"{change * 1000.0:F1}ms\n" +
                         $"{this.CurrentIndex:000} / {this._states.Count:000}\n" +
                         $"Path: {pathCount} tiles";
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