using NUnit.Framework;
using Paths.Core.Search;
using static Paths.Tests.Search.SearchCorpus;

namespace Paths.Tests.Search {
    public class BestFirstSearchTests {
        private static readonly int[] Sizes = { 10, 16 };
        private static readonly double[] Densities = { 0.22, 0.30 };

        [Test]
        public void OpenGridOrthogonalCostIsManhattan() {
            var grid = new Grid(8, 8);
            int start = grid.Index(new Coord(0, 0));
            int goal = grid.Index(new Coord(7, 7));

            SearchResult result = new AStarSearch().Search(grid, start, goal, new SearchOptions());

            Assert.IsTrue(result.Found);
            Assert.AreEqual(14f, result.Cost, Eps);
        }

        [Test]
        public void OpenGridDiagonalCostIsOctile() {
            var grid = new Grid(8, 8);
            int start = grid.Index(new Coord(0, 0));
            int goal = grid.Index(new Coord(7, 7));

            SearchResult result = new AStarSearch()
                .Search(grid, start, goal, new SearchOptions(true, Heuristic.Octile));

            Assert.IsTrue(result.Found);
            Assert.AreEqual(7f * Grid.DiagonalCost, result.Cost, Eps);
        }

        [Test]
        public void WalledOffGoalIsNotFound() {
            var grid = new Grid(5, 5);
            grid.SetWalkable(grid.Index(new Coord(3, 4)), false);
            grid.SetWalkable(grid.Index(new Coord(4, 3)), false);
            grid.SetWalkable(grid.Index(new Coord(3, 3)), false);

            SearchResult result = new AStarSearch()
                .Search(grid, grid.Index(new Coord(0, 0)), grid.Index(new Coord(4, 4)), new SearchOptions());

            Assert.IsFalse(result.Found);
            Assert.AreEqual(float.PositiveInfinity, result.Cost);
        }

        [Test]
        public void StartEqualsGoalIsAPathOfOne() {
            var grid = new Grid(4, 4);
            int cell = grid.Index(new Coord(1, 1));

            SearchResult result = new AStarSearch().Search(grid, cell, cell, new SearchOptions());

            Assert.IsTrue(result.Found);
            CollectionAssert.AreEqual(new[] { cell }, result.Path);
            Assert.AreEqual(0f, result.Cost, Eps);
        }

        /// <summary>
        /// The one that matters. Every search that claims the shortest path has to agree with the
        /// reference on exact cost and on whether a path exists at all, across the whole corpus.
        /// Manhattan is paired only with orthogonal movement, since it overestimates diagonals.
        /// </summary>
        [TestCase("astar-manhattan", Heuristic.Manhattan, false)]
        [TestCase("astar-octile", Heuristic.Octile, true)]
        [TestCase("astar-euclidean", Heuristic.Euclidean, true)]
        [TestCase("astar-chebyshev", Heuristic.Chebyshev, true)]
        [TestCase("dijkstra-orthogonal", Heuristic.Manhattan, false)]
        [TestCase("dijkstra-diagonal", Heuristic.Manhattan, true)]
        public void MatchesTheReferenceOptimum(string label, Heuristic heuristic, bool diagonal) {
            bool dijkstra = label.StartsWith("dijkstra");

            foreach (int size in Sizes)
                foreach (double density in Densities)
                    for (int seed = 0; seed < 60; seed++) {
                        Grid grid = Seeded(seed, size, size, density);
                        int start = grid.Index(new Coord(0, 0));
                        int goal = grid.Index(new Coord(size - 1, size - 1));
                        float reference = ReferenceCost(grid, start, goal, diagonal);

                        ISearch search = dijkstra ? (ISearch) new DijkstraSearch() : new AStarSearch();
                        SearchResult result = search.Search(grid, start, goal, new SearchOptions(diagonal, heuristic));

                        string where = $"{label}, size {size}, density {density}, seed {seed}{Render(grid, start, goal)}";
                        Assert.AreEqual(reference >= 0, result.Found, $"reachability mismatch on {where}");
                        if (reference >= 0)
                            Assert.AreEqual(reference, result.Cost, Eps, $"cost mismatch on {where}");
                        AssertPathIsWalkable(grid, result, start, goal, diagonal, where);
                    }
        }

        // greedy gives up optimality for speed, but it still has to come back with a real path
        [Test]
        public void GreedyFindsAWalkablePathWhereverOneExists() {
            foreach (int size in Sizes)
                foreach (double density in Densities)
                    for (int seed = 0; seed < 60; seed++) {
                        Grid grid = Seeded(seed, size, size, density);
                        int start = grid.Index(new Coord(0, 0));
                        int goal = grid.Index(new Coord(size - 1, size - 1));
                        float reference = ReferenceCost(grid, start, goal, true);

                        SearchResult result = new GreedySearch()
                            .Search(grid, start, goal, new SearchOptions(true, Heuristic.Octile));

                        string where = $"greedy, size {size}, density {density}, seed {seed}{Render(grid, start, goal)}";
                        Assert.AreEqual(reference >= 0, result.Found, $"reachability mismatch on {where}");
                        AssertPathIsWalkable(grid, result, start, goal, true, where);
                        if (reference >= 0)
                            Assert.GreaterOrEqual(result.Cost, reference - Eps, $"beat the true optimum on {where}");
                    }
        }

        // walking a heavier cell should cost what the cell charges, and a detour around it should win
        [Test]
        public void WeightsChangeWhichRouteWins() {
            var grid = new Grid(3, 3);
            int start = grid.Index(new Coord(0, 1));
            int goal = grid.Index(new Coord(2, 1));
            grid.SetWeight(grid.Index(new Coord(1, 1)), 8f);

            SearchResult result = new DijkstraSearch().Search(grid, start, goal, new SearchOptions());

            Assert.IsTrue(result.Found);
            Assert.AreEqual(4f, result.Cost, Eps, "should detour around the expensive middle cell");
            CollectionAssert.DoesNotContain(result.Path, grid.Index(new Coord(1, 1)));
        }
    }
}
