using NUnit.Framework;
using Paths.Core.Search;
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

        [Test]
        public void WallsAreLeftAlone() {
            var (grid, result, start, goal) = Solve(7, 12, 0.3);
            ChangeController trace = SearchTrace.Build(grid, result, start, goal);
            trace.Forward(trace.Count);

            for (int i = 0; i < grid.CellCount; i++) {
                if (grid.IsWalkable(i)) continue;
                Coord at = grid.At(i);
                Assert.AreEqual(GridNodeType.Wall, trace.Current[at.X, at.Y], $"wall at {at} was painted over");
            }
        }
    }
}
