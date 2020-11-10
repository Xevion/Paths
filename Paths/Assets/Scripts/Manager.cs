using System;
using System.Collections.Generic;
using System.Linq;
using Algorithms;
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

    public void Start() {
        GeneratePath();
        _states = new List<GridState>();
    }

    public void Update() {
        if (_curIndex < _states.Count)
            this.LoadNextState();
        else {
            float t1 = Time.time, t2 = Time.time;
            try {
                t1 = Time.time;
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

        foreach (int index in Enumerable.Range(0, 300))
            nodeGrid.FlipRandomWall();

        path = _algorithm.FindPath(start, end);

        _states = _algorithm.GetStates();
    }

    private void LoadNextState() {
        GridState state = _states[_curIndex];
        gridController.LoadGridState(state);
        _curIndex += 2;
    }
}