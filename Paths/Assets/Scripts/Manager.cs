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
    public GridController gridController;
    private IPathfinding _algorithm;
    private List<GridState> _states;
    private int _curIndex;
    private Stack<Node> path;
    private float lastStart;
    public TextMeshPro debugText;

    public void Start() {
        _states = new List<GridState>();
        GeneratePath();
    }

    public void OnDrawGizmos() {
        Gizmos.DrawSphere(transform.position, 1);
    }

    public void Update() {
        if (_curIndex < _states.Count)
            this.LoadNextState();
        else {
            try {
                lastStart = Time.realtimeSinceStartup;
                GeneratePath();
                _curIndex = 0;
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

        int wallCount = (int) (gridController.size * gridController.size * 0.5);
        foreach (int unused in Enumerable.Range(0, wallCount))
            nodeGrid.FlipRandomWall();

        path = _algorithm.FindPath(start, end);

        _states = _algorithm.GetStates();
    }

    private void LoadNextState() {
        GridState state = _states[_curIndex];
        gridController.LoadGridState(state);

        float change = state.time - lastStart;
        string pathCount = path != null ? $"{path.Count}" : "N/A";
        debugText.text = $"{change * 1000.0:F1}ms\n{this._curIndex:000} / {this._states.Count:000}\nPath: {pathCount} tiles";
        _curIndex += 1;
    }
}