using Algorithms;
using BenchmarkDotNet.Attributes;

namespace Paths.Benchmarks {
    /// <summary>
    /// Neighbor enumeration runs once per expanded node, so it sits directly under every search.
    /// Worth its own number to tell "the frontier is slow" apart from "walking the grid is slow".
    /// </summary>
    [MemoryDiagnoser]
    public class NeighborsBenchmark {
        [Params(128)]
        public int Size;

        private NodeGrid _grid;

        [GlobalSetup]
        public void Setup() => _grid = Grids.Seeded(Size, 0.22);

        [Benchmark(Baseline = true)]
        public int Orthogonal() => Count(false);

        [Benchmark]
        public int Diagonal() => Count(true);

        private int Count(bool diagonal) {
            int seen = 0;
            foreach (Node node in _grid.Iterator())
                foreach (Node _ in _grid.Neighbors(node, diagonal))
                    seen++;
            return seen;
        }
    }
}
