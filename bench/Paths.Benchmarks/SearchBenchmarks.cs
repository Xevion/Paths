using System.Collections.Generic;
using Algorithms;
using BenchmarkDotNet.Attributes;
using UnityEngine;

namespace Paths.Benchmarks {
    /// <summary>
    /// One full solve per operation: build the search, find the path, clean the grid back up.
    /// A*, Dijkstra and Greedy all share BestFirst's sorted-list frontier, so most of what these
    /// measure is how that insert holds up as the grid grows.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, targetCount: 5)]
    public class SearchBenchmarks {
        [Params(32, 128, 256)]
        public int Size;

        private NodeGrid _grid;
        private Vector2Int _start;
        private Vector2Int _end;

        [GlobalSetup]
        public void Setup() {
            _grid = Grids.Seeded(Size, 0.22);
            _start = new Vector2Int(0, 0);
            _end = new Vector2Int(Size - 1, Size - 1);
        }

        [Benchmark(Baseline = true)]
        public int AStar() => Solve(new AStar(_grid, Heuristic.Euclidean, true));

        [Benchmark]
        public int Dijkstra() => Solve(new Dijkstra(_grid, true));

        [Benchmark]
        public int Greedy() => Solve(new Greedy(_grid, Heuristic.Euclidean, true));

        [Benchmark]
        public int JumpPoint() => Solve(new JumpPoint(_grid));

        [Benchmark]
        public int Bidirectional() => Solve(new Bidirectional(_grid, true));

        private int Solve(IPathfinding search) {
            Stack<Node> path = search.FindPath(_start, _end);
            search.Cleanup();
            return path?.Count ?? 0;
        }
    }
}
