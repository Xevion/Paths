using System.Collections.Generic;
using Algorithms;
using LevelGeneration;
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
    public float Weight { get; set; } = 1f; // A* heuristic weight (1 = optimal)
    public bool UseBidirectional { get; set; } // overrides Algorithm with a two-ended Dijkstra

    private IPathfinding _algorithm;

    public PathSolver(int width, int height, System.Random random = null) {
        Grid = new NodeGrid(width, height, random);
        Start = Grid.RandomPosition();
        End = Grid.RandomPosition();
    }

    /// <summary>
    /// Build a grid from a seed + generator: the same seed always lays out the same walls and start/end.
    /// The generator paints the walls, then start/end are dropped on walkable cells so they never open
    /// buried in a wall.
    /// </summary>
    public PathSolver(int width, int height, int seed, Generator generator, float density) {
        Grid = new NodeGrid(width, height, new System.Random(seed));
        Generate(generator, density);

        Start = RandomWalkable();
        End = RandomWalkable(Start);
    }

    /// <summary>
    /// Paint the starting layout. Rooms and Walk carve out of a solid grid rather than adding to an
    /// open one, so density reads as how much wall to leave behind. Both keep their open space in one
    /// connected region - a single walk, and corridors between rooms - so start and end can always meet.
    /// </summary>
    private void Generate(Generator generator, float density) {
        float open = 1f - density;
        switch (generator) {
            case Generator.Random:
                Grid.ApplyGenerator(new RandomPlacement(density, true, true));
                break;
            case Generator.Rooms:
                Fill();
                int rooms = Mathf.Clamp(Mathf.RoundToInt(Grid.CellCount * open / 90f), 3, 14);
                Grid.ApplyGenerator(new RoomCarve(new Vector2Int(rooms, rooms + 3), new Vector2Int(3, 8), 80));
                break;
            case Generator.Walk:
                Fill();
                // one walk, not several - separate walks would carve caves that never meet
                int steps = Mathf.Max(32, Mathf.RoundToInt(Grid.CellCount * open * 3f));
                Grid.ApplyGenerator(new DrunkardWalk(1, steps, false));
                break;
            // Empty: every cell stays walkable
        }
    }

    private void Fill() {
        foreach (Node node in Grid.Iterator())
            node.Walkable = false;
    }

    // a random walkable cell, so start/end never land on a wall; optionally distinct from another
    private Vector2Int RandomWalkable(Vector2Int? avoid = null) {
        for (int i = 0; i < 500; i++) {
            Vector2Int p = Grid.RandomPosition();
            if (Grid.GetNode(p).Walkable && (!avoid.HasValue || p != avoid.Value))
                return p;
        }
        return Grid.RandomPosition(); // grid packed solid - give up and take anything
    }

    /// <summary>Run the selected algorithm start -> end and hand back the recorded changes to play.</summary>
    public ChangeController Solve() {
        _algorithm?.Cleanup(); // undo the previous run's edits to the node grid
        _algorithm = Create();
        Path = _algorithm.FindPath(Start, End);
        return _algorithm.ChangeController;
    }

    private IPathfinding Create() {
        // bidirectional is a mode, not an entry in the picker - it overrides the chosen algorithm
        if (UseBidirectional)
            return new Bidirectional(Grid, Diagonal);
        switch (Algorithm) {
            case Algorithm.Dijkstra:
                return new Dijkstra(Grid, Diagonal);
            case Algorithm.Greedy:
                return new Greedy(Grid, Heuristic, Diagonal);
            case Algorithm.BreadthFirst:
                return new BreadthFirst(Grid, Diagonal);
            case Algorithm.DepthFirst:
                return new DepthFirst(Grid, Diagonal);
            case Algorithm.JumpPoint:
                return new JumpPoint(Grid); // always 8-connected, the Diagonal toggle doesn't apply
            default:
                return new AStar(Grid, Heuristic, Diagonal, Weight);
        }
    }

    public bool IsValid(Vector2Int position) => Grid.IsValid(position);
    public Node GetNode(Vector2Int position) => Grid.GetNode(position);

    // the plain editable grid (walls + start/end), for when nothing's being animated
    public GridNodeType[,] RenderEditable() => Grid.RenderNodeTypes(Start, End);

    /// <summary>
    /// Per-cell g-cost from the last Solve (distance from start), for the distance heatmap. Only the
    /// search layer reads it, so the node default on untouched cells doesn't matter. Valid until the
    /// next Solve cleans the nodes.
    /// </summary>
    public float[,] CostField() {
        var field = new float[Grid.Width, Grid.Height];
        for (int x = 0; x < Grid.Width; x++)
            for (int y = 0; y < Grid.Height; y++)
                field[x, y] = Grid.Grid[x, y].Cost ?? 0f;
        return field;
    }
}
