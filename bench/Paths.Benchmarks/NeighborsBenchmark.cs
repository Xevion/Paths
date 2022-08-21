using BenchmarkDotNet.Attributes;
using Paths.Core.Search;

namespace Paths.Benchmarks {
    /// <summary>
    /// Neighbour enumeration runs once per expanded cell, so it sits directly under every search.
    /// Worth its own number to tell "the frontier is slow" apart from "walking the grid is slow".
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, targetCount: 5)]
    public class NeighborsBenchmark {
        [Params(128)]
        public int Size;

        private Grid _grid;
        private int[] _buffer;

        [GlobalSetup]
        public void Setup() {
            _grid = Grids.Seeded(Size, 0.22);
            _buffer = new int[Grid.MaxNeighbors];
        }

        [Benchmark(Baseline = true)]
        public int Orthogonal() => Count(false);

        [Benchmark]
        public int Diagonal() => Count(true);

        private int Count(bool diagonal) {
            int seen = 0;
            for (int i = 0; i < _grid.CellCount; i++)
                seen += _grid.Neighbors(i, diagonal, _buffer);
            return seen;
        }
    }
}
