#nullable enable
using System;
using System.Collections.Generic;
using Paths.Core.Generation;
using Paths.Core.Search;

/// <summary>
/// Holds the grid being edited plus the start and end cells, and runs the search over it. Plain C#
/// on purpose: this is the whole of what the app needs from the pathfinding side, so keeping it off
/// MonoBehaviour keeps the algorithms from being welded to the scene. Input pokes at it, and
/// UIController plays back whatever Solve hands over.
/// </summary>
public class PathSolver {
    public Grid Grid { get; }
    public Coord Start { get; set; }
    public Coord End { get; set; }

    /// <summary>Cells on the last path found, start first. Empty when there wasn't one.</summary>
    public IReadOnlyList<int> Path => _result?.Path ?? EmptyPath;

    // what the next Solve uses. set these from the UI, then re-solve
    public Algorithm Algorithm { get; set; } = Algorithm.AStar;
    public Heuristic Heuristic { get; set; } = Heuristic.Manhattan;
    public bool Diagonal { get; set; }
    public float Weight { get; set; } = 1f; // A* heuristic weight, 1 keeps it shortest
    public bool UseBidirectional { get; set; } // overrides Algorithm with a two-ended Dijkstra

    private static readonly int[] EmptyPath = new int[0];

    private readonly Random _random;
    private SearchResult? _result;

    public PathSolver(int width, int height, Random? random = null) {
        _random = random ?? new Random();
        Grid = new Grid(width, height);
        Start = RandomWalkable();
        End = RandomWalkable(Start);
    }

    /// <summary>
    /// Build a grid from a seed and a generator: the same seed always lays out the same walls and
    /// the same start and end.
    /// </summary>
    public PathSolver(int width, int height, int seed, Generator generator, float density) {
        _random = new Random(seed);
        Grid = new Grid(width, height);
        Generate(generator, density);

        Start = RandomWalkable();
        End = RandomWalkable(Start);
    }

    /// <summary>
    /// Paint the starting layout. Rooms and Walk carve out of a solid grid rather than adding to an
    /// open one, so density reads as how much wall to leave behind. Both keep their open space in a
    /// single connected region, so start and end can always meet.
    /// </summary>
    private void Generate(Generator generator, float density) {
        float open = 1f - density;
        switch (generator) {
            case Generator.Random:
                new RandomPlacement(density, true, true).Generate(Grid, _random);
                break;
            case Generator.Rooms:
                Fill();
                int rooms = Clamp((int) Math.Round(Grid.CellCount * open / 90f), 3, 14);
                new RoomCarve(rooms, rooms + 3, 3, 8).Generate(Grid, _random);
                break;
            case Generator.Walk:
                Fill();
                // one walk, not several: separate walks would carve caves that never meet
                int steps = Math.Max(32, (int) Math.Round(Grid.CellCount * open * 3f));
                new DrunkardWalk(1, steps, false).Generate(Grid, _random);
                break;
            // Empty: every cell stays walkable
        }
    }

    private void Fill() {
        for (int i = 0; i < Grid.CellCount; i++)
            Grid.SetWalkable(i, false);
    }

    private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

    // a random walkable cell, so start and end never land in a wall
    private Coord RandomWalkable(Coord? avoid = null) {
        for (int i = 0; i < 500; i++) {
            int cell = _random.Next(Grid.CellCount);
            Coord at = Grid.At(cell);
            if (Grid.IsWalkable(cell) && (!avoid.HasValue || at != avoid.Value))
                return at;
        }
        return Grid.At(_random.Next(Grid.CellCount)); // packed solid, take anything
    }

    /// <summary>Run the selected search and hand back the timeline to play through.</summary>
    public ChangeController Solve() {
        int start = Grid.Index(Start);
        int goal = Grid.Index(End);

        var clock = System.Diagnostics.Stopwatch.StartNew();
        _result = Create().Search(Grid, start, goal, new SearchOptions(Diagonal, Heuristic, Weight));
        clock.Stop();

        return SearchTrace.Build(Grid, _result, start, goal, clock.Elapsed.TotalSeconds);
    }

    private ISearch Create() {
        // bidirectional is a mode rather than an entry in the picker, so it overrides the choice
        if (UseBidirectional)
            return new BidirectionalSearch();

        switch (Algorithm) {
            case Algorithm.Dijkstra: return new DijkstraSearch();
            case Algorithm.Greedy: return new GreedySearch();
            case Algorithm.BreadthFirst: return new BreadthFirstSearch();
            case Algorithm.DepthFirst: return new DepthFirstSearch();
            case Algorithm.JumpPoint: return new JumpPointSearch();
            default: return new AStarSearch();
        }
    }

    public bool IsValid(Coord position) => Grid.InBounds(position);

    public bool IsWalkable(Coord position) => Grid.IsWalkable(Grid.Index(position));

    public void SetWalkable(Coord position, bool walkable) => Grid.SetWalkable(Grid.Index(position), walkable);

    /// <summary>The plain editable grid, walls plus start and end, for when nothing is animating.</summary>
    public GridNodeType[,] RenderEditable() => SearchTrace.Render(Grid, Grid.Index(Start), Grid.Index(End));

    /// <summary>
    /// Per-cell distance from the start as of the last Solve, for the heatmap. Cells the search
    /// never reached come back at zero, which is what the shader treats as no reading.
    /// </summary>
    public float[,] CostField() {
        var field = new float[Grid.Width, Grid.Height];
        if (_result == null)
            return field;

        for (int i = 0; i < Grid.CellCount; i++) {
            float cost = _result.State.Cost(i);
            if (float.IsPositiveInfinity(cost))
                continue;
            Coord at = Grid.At(i);
            field[at.X, at.Y] = cost;
        }
        return field;
    }
}
