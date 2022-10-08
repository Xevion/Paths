using BenchmarkDotNet.Attributes;
using Paths.Core.Search;

namespace Paths.Benchmarks {
    /// <summary>
    /// The question the incremental search exists to answer: after one cell changes, is repairing the
    /// plan cheaper than working it out again? Both measure the same thing, the cost of having a
    /// correct route after an edit, so they are directly comparable.
    ///
    /// The edit is toggled back and forth on the same cell so the grid stays the same shape run to
    /// run, and it sits mid-board where it has a real chance of disturbing the route.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, targetCount: 5)]
    public class ReplanBenchmarks {
        [Params(32, 128, 256)]
        public int Size;

        private Grid _grid;
        private int _start;
        private int _goal;
        private int _edit;
        private SearchOptions _options;
        private DStarLite _incremental;

        [GlobalSetup]
        public void Setup() {
            _grid = Grids.Seeded(Size, 0.22);
            _start = _grid.Index(new Coord(0, 0));
            _goal = _grid.Index(new Coord(Size - 1, Size - 1));
            _edit = _grid.Index(new Coord(Size / 2, Size / 2));
            _options = new SearchOptions(true, Heuristic.Octile);

            _incremental = new DStarLite();
            _incremental.Start(_grid, _start, _goal, _options);
        }

        private void Toggle() {
            _grid.SetWalkable(_edit, !_grid.IsWalkable(_edit));
        }

        /// <summary>Edit, then solve from nothing. What the app did before this existed.</summary>
        [Benchmark(Baseline = true)]
        public int FullSolve() {
            Toggle();
            return new AStarSearch().Search(_grid, _start, _goal, _options).Path.Count;
        }

        /// <summary>Edit, then repair the plan already held.</summary>
        [Benchmark]
        public int Replan() {
            Toggle();
            _incremental.Touch(new[] { _edit });
            return _incremental.Replan().Path.Count;
        }

        /// <summary>
        /// The first plan, for scale. It costs more than a plain A* because it settles every cell
        /// rather than stopping at the goal, which is what every later repair is borrowing against.
        /// </summary>
        [Benchmark]
        public int FirstPlan() => new DStarLite().Start(_grid, _start, _goal, _options).Path.Count;
    }
}
