using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Algorithms;
using Vector2 = System.Numerics.Vector2;
using UnityEngine;

/// <summary>
/// The primary controller of the entire application, managing state, events and sending commands
/// </summary>
public class Manager : MonoBehaviour {
    public GridController gridController;
    private IPathfinding _algorithm;
    
    public void Start() {
        var nodeGrid = new NodeGrid(gridController.size, gridController.size);
        _algorithm = new AStar(nodeGrid);
        
        Vector2 start = new Vector2(0, 0);
        Vector2 end = new Vector2(16, 16);
        
        foreach(int index in Enumerable.Range(0, 150))
            nodeGrid.FlipRandomWall();
        
        Stack<Node> path = _algorithm.FindPath(start, end);
        
        List<GridState> states = _algorithm.GetStates();
        gridController.LoadGridState(states[states.Count - 1]);
        Debug.Log(states[states.Count - 1].RenderGrid());
        Debug.Log(start);
        Debug.Log(end);
        
        // StartCoroutine(LateStart());
    }

    // IEnumerator LateStart() {
    // }
}