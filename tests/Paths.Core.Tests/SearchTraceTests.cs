using NUnit.Framework;
using Paths.Core;
using Paths.Core.Search;
using Paths.Tests.Search;
using static Paths.Tests.Search.SearchCorpus;

namespace Paths.Tests {
    public class SearchTraceTests {
        private static (Grid grid, SearchResult result, int start, int goal) Solve(int seed, int size, double density) {
            Grid grid = Seeded(seed, size, size, density);
            int start = grid.Index(new Coord(0, 0));
            int goal = grid.Index(new Coord(size - 1, size - 1));
            SearchResult result = new AStarSearch()
                .Search(grid, start, goal, new SearchOptions(true, Heuristic.Octile));
            return (grid, result, start, goal);
        }

        [Test]
        public void InitialPictureHasWallsAndBothEndpoints() {
            var grid = new Grid(4, 3);
            grid.SetWalkable(grid.Index(new Coord(1, 1)), false);
            int start = grid.Index(new Coord(0, 0));
            int goal = grid.Index(new Coord(3, 2));

            GridNodeType[,] rendered = SearchTrace.Render(grid, start, goal);

            Assert.AreEqual(GridNodeType.Start, rendered[0, 0]);
            Assert.AreEqual(GridNodeType.End, rendered[3, 2]);
            Assert.AreEqual(GridNodeType.Wall, rendered[1, 1]);
            Assert.AreEqual(GridNodeType.Empty, rendered[2, 0]);
        }

        [Test]
        public void StartAndEndAreNeverOverwritten() {
            var (grid, result, start, goal) = Solve(3, 12, 0.2);
            ChangeController trace = SearchTrace.Build(grid, result, start, goal);

            trace.Forward(trace.Count);

            Coord startAt = grid.At(start);
            Coord goalAt = grid.At(goal);
            Assert.AreEqual(GridNodeType.Start, trace.Current[startAt.X, startAt.Y]);
            Assert.AreEqual(GridNodeType.End, trace.Current[goalAt.X, goalAt.Y]);
        }

        // every change has to describe what it actually replaced, or scrubbing backwards restores
        // the wrong picture
        [Test]
        public void EveryChangeReplacesWhatWasThere() {
            var (grid, result, start, goal) = Solve(5, 14, 0.24);
            ChangeController trace = SearchTrace.Build(grid, result, start, goal);

            var current = SearchTrace.Render(grid, start, goal);
            for (int i = 0; i < trace.Count; i++) {
                trace.Forward();
                Change change = trace.CurrentChange;
                Assert.AreEqual(current[change.X, change.Y], change.Old,
                    $"change {i} at ({change.X}, {change.Y}) claims the wrong previous value");
                current[change.X, change.Y] = change.New;
            }
        }

        /// <summary>
        /// The property the whole timeline rests on: play it all the way out and wind it all the
        /// way back, and the grid has to be exactly where it started.
        /// </summary>
        [Test]
        public void PlayingForwardThenBackRestoresTheInitialPicture() {
            foreach (int seed in new[] { 1, 2, 3, 4, 5 }) {
                var (grid, result, start, goal) = Solve(seed, 14, 0.24);
                ChangeController trace = SearchTrace.Build(grid, result, start, goal);
                GridNodeType[,] before = SearchTrace.Render(grid, start, goal);

                trace.Forward(trace.Count);
                trace.Reverse(trace.Count);

                for (int x = 0; x < grid.Width; x++)
                    for (int y = 0; y < grid.Height; y++)
                        Assert.AreEqual(before[x, y], trace.Current[x, y], $"seed {seed} at ({x}, {y})");
            }
        }

        [Test]
        public void PlayingRightThroughLeavesThePathDrawn() {
            var (grid, result, start, goal) = Solve(2, 12, 0.18);
            Assume.That(result.Found);

            ChangeController trace = SearchTrace.Build(grid, result, start, goal);
            trace.Forward(trace.Count);

            foreach (int cell in result.Path) {
                if (cell == start || cell == goal) continue;
                Coord at = grid.At(cell);
                Assert.AreEqual(GridNodeType.Path, trace.Current[at.X, at.Y], $"cell {at} should be on the path");
            }
        }

        /// <summary>
        /// Every search, not just the ones that happen to keep off the walls. Edits and repairs are
        /// EditedTraceTests' business; this pins the first plan for all of them.
        /// </summary>
        [TestCaseSource(typeof(Searches), nameof(Searches.All))]
        public void StructuralCellsSurviveEverySearch(string name, ISearch search) {
            foreach (bool diagonal in new[] { false, true })
                foreach (int seed in new[] { 3, 7, 11 }) {
                    Grid grid = Seeded(seed, 12, 12, 0.3);
                    int start = grid.Index(new Coord(0, 0));
                    int goal = grid.Index(new Coord(11, 11));
                    SearchResult result = search.Search(grid, start, goal,
                        new SearchOptions(diagonal, Heuristic.Octile));

                    ChangeController trace = SearchTrace.Build(grid, result, start, goal);
                    trace.Forward(trace.Count);

                    AssertStructureIntact(grid, trace, start, goal, $"{name} seed {seed} diagonal {diagonal}");
                }
        }

        internal static void AssertStructureIntact(Grid grid, ChangeController trace, int start, int goal, string where) {
            for (int i = 0; i < grid.CellCount; i++) {
                Coord at = grid.At(i);
                GridNodeType drawn = trace.Current[at.X, at.Y];
                if (i == start)
                    Assert.AreEqual(GridNodeType.Start, drawn, $"start was painted over on {where}");
                else if (i == goal)
                    Assert.AreEqual(GridNodeType.End, drawn, $"end was painted over on {where}");
                else if (!grid.IsWalkable(i))
                    Assert.AreEqual(GridNodeType.Wall, drawn,
                        $"wall at {at} was painted over on {where}{Render(grid, start, goal)}");
                else
                    Assert.AreNotEqual(GridNodeType.Wall, drawn, $"open cell {at} drawn as a wall on {where}");
            }
        }
    }
}
