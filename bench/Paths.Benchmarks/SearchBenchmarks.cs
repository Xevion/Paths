using BenchmarkDotNet.Attributes;
using Paths.Core.Search;

namespace Paths.Benchmarks {
    /// <summary>
    /// One full solve per operation, corner to corner over a seeded 22% wall scatter.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, targetCount: 5)]
    public class SearchBenchmarks {
        [Params(32, 128, 256)]
        public int Size;

        private Grid _grid;
        private int _start;
        private int _goal;
        private SearchOptions _options;

        [GlobalSetup]
        public void Setup() {
            _grid = Grids.Seeded(Size, 0.22);
            _start = _grid.Index(new Coord(0, 0));
            _goal = _grid.Index(new Coord(Size - 1, Size - 1));
            _options = new SearchOptions(true, Heuristic.Octile);
        }

        private int Solve(ISearch search) => search.Search(_grid, _start, _goal, _options).Path.Count;

        [Benchmark(Baseline = true)]
        public int AStar() => Solve(new AStarSearch());

        [Benchmark]
        public int Dijkstra() => Solve(new DijkstraSearch());

        [Benchmark]
        public int Greedy() => Solve(new GreedySearch());

        [Benchmark]
        public int JumpPoint() => Solve(new JumpPointSearch());

        [Benchmark]
        public int Bidirectional() => Solve(new BidirectionalSearch());
    }
}
