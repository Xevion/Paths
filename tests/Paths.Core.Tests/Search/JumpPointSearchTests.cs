using NUnit.Framework;
using Paths.Core.Search;
using static Paths.Tests.Search.SearchCorpus;

namespace Paths.Tests.Search {
    public class JumpPointSearchTests {
        [Test]
        public void OpenGridIsAStraightDiagonal() {
            var grid = new Grid(8, 8);
            int start = grid.Index(new Coord(0, 0));
            int goal = grid.Index(new Coord(7, 7));

            SearchResult result = new JumpPointSearch().Search(grid, start, goal, new SearchOptions());

            Assert.IsTrue(result.Found);
            Assert.AreEqual(7f * Grid.DiagonalCost, result.Cost, Eps);
        }

        /// <summary>
        /// The battery that caught the corner-cutting bug the first time around. JPS has to agree
        /// with the reference on exact cost and reachability everywhere, and the path it hands back
        /// has to be filled in cell by cell rather than left as a chain of jump points.
        /// </summary>
        [Test]
        public void MatchesTheReferenceOptimum() {
            foreach (int size in new[] { 10, 16 })
                foreach (double density in new[] { 0.22, 0.30 })
                    for (int seed = 0; seed < 60; seed++) {
                        Grid grid = Seeded(seed, size, size, density);
                        int start = grid.Index(new Coord(0, 0));
                        int goal = grid.Index(new Coord(size - 1, size - 1));
                        float reference = ReferenceCost(grid, start, goal, true);

                        SearchResult result = new JumpPointSearch().Search(grid, start, goal, new SearchOptions());

                        string where = $"size {size}, density {density}, seed {seed}{Render(grid, start, goal)}";
                        Assert.AreEqual(reference >= 0, result.Found, $"reachability mismatch on {where}");
                        if (reference >= 0)
                            Assert.AreEqual(reference, result.Cost, Eps, $"cost mismatch on {where}");
                        AssertPathIsWalkable(grid, result, start, goal, true, where);
                    }
        }

        // non-square grids are where a row-major mixup would show up
        [Test]
        public void MatchesTheReferenceOnNonSquareGrids() {
            for (int seed = 0; seed < 40; seed++) {
                Grid grid = Seeded(seed, 13, 7, 0.25);
                int start = grid.Index(new Coord(0, 0));
                int goal = grid.Index(new Coord(12, 6));
                float reference = ReferenceCost(grid, start, goal, true);

                SearchResult result = new JumpPointSearch().Search(grid, start, goal, new SearchOptions());

                string where = $"13x7, seed {seed}{Render(grid, start, goal)}";
                Assert.AreEqual(reference >= 0, result.Found, $"reachability mismatch on {where}");
                if (reference >= 0)
                    Assert.AreEqual(reference, result.Cost, Eps, $"cost mismatch on {where}");
                AssertPathIsWalkable(grid, result, start, goal, true, where);
            }
        }

        // the whole point: it should reach the same answer having expanded far fewer cells
        [Test]
        public void ExpandsFewerCellsThanAStar() {
            var grid = new Grid(40, 40);
            int start = grid.Index(new Coord(0, 0));
            int goal = grid.Index(new Coord(39, 39));
            var options = new SearchOptions(true, Heuristic.Octile);

            SearchResult jps = new JumpPointSearch().Search(grid, start, goal, options);
            SearchResult astar = new AStarSearch().Search(grid, start, goal, options);

            Assert.AreEqual(astar.Cost, jps.Cost, Eps);
            Assert.Less(jps.Expanded, astar.Expanded);
        }

        [Test]
        public void WalledOffGoalIsNotFound() {
            var grid = new Grid(6, 6);
            foreach (Coord wall in new[] { new Coord(4, 5), new Coord(5, 4), new Coord(4, 4) })
                grid.SetWalkable(grid.Index(wall), false);

            SearchResult result = new JumpPointSearch()
                .Search(grid, grid.Index(new Coord(0, 0)), grid.Index(new Coord(5, 5)), new SearchOptions());

            Assert.IsFalse(result.Found);
        }
    }
}
