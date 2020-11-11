using System;
using System.Collections.Generic;
using System.Linq;
using Algorithms;
using TMPro;
using UnityEngine;

/// <summary>
/// The primary controller of the entire application, managing state, events and sending commands
/// </summary>
public class Manager : MonoBehaviour {
    private IPathfinding _algorithm;
    private List<GridState> _states;
    private int _curIndex;
    private Stack<Node> path;
    private float _lastStart;
    private float _runtime;

    public GridController gridController;
    public TextMeshPro debugText;
    public float speed;

    public int CurrentIndex {
        get => (int) _runtime;
        set => _runtime = value;
    }

    public void Start() {
        _states = new List<GridState>();
        GeneratePath();
    }

    public void OnDrawGizmos() {
        float size = (float) (10.0 / gridController.size);
        Gizmos.DrawWireCube(transform.position, new Vector3(size, size, size));
    }

    public void Update() {
        _runtime += Time.deltaTime * speed;

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
        var nodeGrid = new NodeGrid(gridController.size, gridController.size);
        _algorithm = new AStar(nodeGrid);

        // Vector2 start = nodeGrid.RandomPosition();
        Vector2Int start = new Vector2Int(30, 30);
        Vector2Int end = nodeGrid.RandomPosition();


        int wallCount = (int) (gridController.size * gridController.size * 0.25);
        for (int unused = 0; unused < wallCount; unused++)
            nodeGrid.AddRandomWall();

        nodeGrid.GetNode(start).Walkable = true;
        nodeGrid.GetNode(end).Walkable = true;

        path = _algorithm.FindPath(start, end);

        _states = _algorithm.GetStates();
    }

    private void LoadNextState() {
        GridState state = _states[CurrentIndex];
        gridController.LoadGridState(state);

        float change = state.Time - _lastStart;
        string pathCount = path != null ? $"{path.Count}" : "N/A";
        debugText.text =
            $"{change * 1000.0:F1}ms\n{this.CurrentIndex:000} / {this._states.Count:000}\nPath: {pathCount} tiles";
    }
}