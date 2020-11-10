using System;
using System.Collections.Generic;
using System.Linq;
using Algorithms;
using TMPro;
using UnityEditor;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

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
        GeneratePath();
        _states = new List<GridState>();
    }

    public void OnDrawGizmos() {
        Gizmos.DrawSphere(transform.position, 1);
    }

    public void Update() {
        if (_curIndex < _states.Count)
            this.LoadNextState();
        else {
            float t1 = Time.time, t2 = Time.time;
            try {
                t1 = Time.time;
                lastStart = Time.realtimeSinceStartup;
                GeneratePath();
                _curIndex = 0;
                Debug.Log($"({NodeGrid.Manhattan(_algorithm.Start, _algorithm.End)} in {_states.Count} states. {path.Count} path length.");
                t2 = Time.time;
                // Debug.Log(t2 - t1);
            }
            catch (ArgumentOutOfRangeException e) {
            }
        }
    }

    private void GeneratePath() {
        var nodeGrid = new NodeGrid(gridController.size, gridController.size);
        _algorithm = new AStar(nodeGrid);

        Vector2 start = nodeGrid.RandomPosition();
        Vector2 end = nodeGrid.RandomPosition();
        // Vector2 start = new Vector2(1, 1);
        // Vector2 end = new Vector2(gridController.size - 5, gridController.size - 5);

        int wallCount = (int) (gridController.size * gridController.size * 0.1);
        foreach (int index in Enumerable.Range(0, wallCount))
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