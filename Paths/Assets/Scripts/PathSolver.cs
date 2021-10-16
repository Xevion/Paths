using System.Collections.Generic;
using Algorithms;
using UnityEngine;

/// <summary>
/// Holds the grid being edited plus the start/end nodes, and runs the pathfinding over it.
/// This part doesn't actually need to be a MonoBehaviour, so it's plain C# - keeps the algorithm
/// side from being welded to UIController and the scene. Input pokes at it, UIController plays
/// back whatever Solve() hands over.
/// </summary>
public class PathSolver {
    public NodeGrid Grid { get; }
    public Vector2Int Start { get; set; }
    public Vector2Int End { get; set; }
    public Stack<Node> Path { get; private set; }

    // what the next Solve() uses - set these from the UI, then re-solve
    public Algorithm Algorithm { get; set; } = Algorithm.AStar;
    public Heuristic Heuristic { get; set; } = Heuristic.Manhattan;
    public bool Diagonal { get; set; }

    private IPathfinding _algorithm;

    public PathSolver(int width, int height) {
        Grid = new NodeGrid(width, height);
        Start = Grid.RandomPosition();
        End = Grid.RandomPosition();
    }

    /// <summary>Run the selected algorithm start -> end and hand back the recorded changes to play.</summary>
    public ChangeController Solve() {
        _algorithm?.Cleanup(); // undo the previous run's edits to the node grid
        _algorithm = Create();
        Path = _algorithm.FindPath(Start, End);
        return _algorithm.ChangeController;
    }

    private IPathfinding Create() {
        switch (Algorithm) {
            case Algorithm.Dijkstra:
                return new Dijkstra(Grid, Diagonal);
            case Algorithm.Greedy:
                return new Greedy(Grid, Heuristic, Diagonal);
            case Algorithm.BreadthFirst:
                return new BreadthFirst(Grid, Diagonal);
            case Algorithm.DepthFirst:
                return new DepthFirst(Grid, Diagonal);
            default:
                return new AStar(Grid, Heuristic, Diagonal);
        }
    }

    public bool IsValid(Vector2Int position) => Grid.IsValid(position);
    public Node GetNode(Vector2Int position) => Grid.GetNode(position);

    // the plain editable grid (walls + start/end), for when nothing's being animated
    public GridNodeType[,] RenderEditable() => Grid.RenderNodeTypes(Start, End);
}
