using System.Collections.Generic;
using Algorithms;
using BenchmarkDotNet.Attributes;
using Paths.Core.Search;
using UnityEngine;
using Grid = Paths.Core.Search.Grid;
using Heuristic = Paths.Core.Search.Heuristic;

namespace Paths.Benchmarks {
    /// <summary>
    /// The same search, old model against new, over identical grids. The old one keeps its state on
    /// shared node objects and orders a sorted list; the new one uses flat arrays and the heap.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, targetCount: 5)]
    public class PortedSearchBenchmarks {
        [Params(32, 128, 256)]
        public int Size;

        private NodeGrid _nodeGrid;
        private Grid _grid;
        private Vector2Int _startAt;
        private Vector2Int _goalAt;
        private int _start;
        private int _goal;
        private SearchOptions _options;

        [GlobalSetup]
        public void Setup() {
            _nodeGrid = Grids.Seeded(Size, 0.22);
            _startAt = new Vector2Int(0, 0);
            _goalAt = new Vector2Int(Size - 1, Size - 1);

            // mirror the walls exactly so both searches face the same problem
            _grid = new Grid(Size, Size);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    _grid.SetWalkable(_grid.Index(new Coord(x, y)), _nodeGrid.Grid[x, y].Walkable);

            _start = _grid.Index(new Coord(0, 0));
            _goal = _grid.Index(new Coord(Size - 1, Size - 1));
            _options = new SearchOptions(true, Heuristic.Euclidean);
        }

        [Benchmark(Baseline = true)]
        public int OldAStar() {
            var search = new AStar(_nodeGrid, Algorithms.Heuristic.Euclidean, true);
            Stack<Node> path = search.FindPath(_startAt, _goalAt);
            search.Cleanup();
            return path?.Count ?? 0;
        }

        [Benchmark]
        public int NewAStar() => new AStarSearch().Search(_grid, _start, _goal, _options).Path.Count;

        [Benchmark]
        public int NewDijkstra() => new DijkstraSearch().Search(_grid, _start, _goal, _options).Path.Count;
    }
}
