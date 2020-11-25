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
    private ChangeController _state;
    private int _curIndex;
    private Stack<Node> _path;
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
            increment = Mathf.Clamp(increment, 0, _state.Count * Time.deltaTime / clampIncrement);
        _runtime += increment;

        if (CurrentIndex < _state.Count)
            LoadNextState();
        else {
            GeneratePath();
            CurrentIndex = 0;
        }
    }

    private void GeneratePath() {
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