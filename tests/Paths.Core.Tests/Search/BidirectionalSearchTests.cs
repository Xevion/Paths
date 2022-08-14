using NUnit.Framework;
using Paths.Core.Search;
using static Paths.Tests.Search.SearchCorpus;

namespace Paths.Tests.Search {
    public class BidirectionalSearchTests {
        [Test]
        public void OpenGridOrthogonalCostIsManhattan() {
            var grid = new Grid(8, 8);
            int start = grid.Index(new Coord(0, 0));
            int goal = grid.Index(new Coord(7, 7));

            SearchResult result = new BidirectionalSearch().Search(grid, start, goal, new SearchOptions());

            Assert.IsTrue(result.Found);
            Assert.AreEqual(14f, result.Cost, Eps);
        }

        [Test]
        public void StartEqualsGoalIsAPathOfOne() {
            var grid = new Grid(4, 4);
            int cell = grid.Index(new Coord(2, 2));

            SearchResult result = new BidirectionalSearch().Search(grid, cell, cell, new SearchOptions());

            Assert.IsTrue(result.Found);
            CollectionAssert.AreEqual(new[] { cell }, result.Path);
            Assert.AreEqual(0f, result.Cost, Eps);
        }

        /// <summary>
        /// Both connectivities against the reference. Stitching two half-paths across a meeting edge
        /// is where this one goes wrong, so the walkability check matters as much as the cost.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void MatchesTheReferenceOptimum(bool diagonal) {
            foreach (int size in new[] { 10, 16 })
                foreach (double density in new[] { 0.22, 0.30 })
                    for (int seed = 0; seed < 60; seed++) {
                        Grid grid = Seeded(seed, size, size, density);
                        int start = grid.Index(new Coord(0, 0));
                        int goal = grid.Index(new Coord(size - 1, size - 1));
                        float reference = ReferenceCost(grid, start, goal, diagonal);

                        SearchResult result = new BidirectionalSearch()
                            .Search(grid, start, goal, new SearchOptions(diagonal));

                        string where = $"diagonal {diagonal}, size {size}, density {density}, seed {seed}{Render(grid, start, goal)}";
                        Assert.AreEqual(reference >= 0, result.Found, $"reachability mismatch on {where}");
                        if (reference >= 0)
                            Assert.AreEqual(reference, result.Cost, Eps, $"cost mismatch on {where}");
                        AssertPathIsWalkable(grid, result, start, goal, diagonal, where);
                    }
        }

        [Test]
        public void MatchesTheReferenceOnNonSquareGrids() {
            for (int seed = 0; seed < 40; seed++) {
                Grid grid = Seeded(seed, 13, 7, 0.25);
                int start = grid.Index(new Coord(0, 0));
                int goal = grid.Index(new Coord(12, 6));
                float reference = ReferenceCost(grid, start, goal, true);

                SearchResult result = new BidirectionalSearch().Search(grid, start, goal, new SearchOptions(true));

                string where = $"13x7, seed {seed}{Render(grid, start, goal)}";
                Assert.AreEqual(reference >= 0, result.Found, $"reachability mismatch on {where}");
                if (reference >= 0)
                    Assert.AreEqual(reference, result.Cost, Eps, $"cost mismatch on {where}");
                AssertPathIsWalkable(grid, result, start, goal, true, where);
            }
        }

        // weights are priced on the cell being entered, and the backward wave walks edges the wrong
        // way round, so this is where an unreversed step cost would show up
        [Test]
        public void HonoursWeightsFromBothDirections() {
            for (int seed = 0; seed < 40; seed++) {
                Grid grid = Seeded(seed, 12, 12, 0.18);
                var rng = new System.Random(seed + 500);
                for (int i = 0; i < grid.CellCount; i++)
                    if (grid.IsWalkable(i) && rng.NextDouble() < 0.3)
                        grid.SetWeight(i, 1f + (float) rng.NextDouble() * 4f);

                int start = grid.Index(new Coord(0, 0));
                int goal = grid.Index(new Coord(11, 11));
                float reference = ReferenceCost(grid, start, goal, true);

                SearchResult result = new BidirectionalSearch().Search(grid, start, goal, new SearchOptions(true));

                string where = $"weighted, seed {seed}{Render(grid, start, goal)}";
                Assert.AreEqual(reference >= 0, result.Found, $"reachability mismatch on {where}");
                if (reference >= 0)
                    Assert.AreEqual(reference, result.Cost, Eps, $"cost mismatch on {where}");
                AssertPathIsWalkable(grid, result, start, goal, true, where);
            }
        }

        [Test]
        public void WalledOffGoalIsNotFound() {
            var grid = new Grid(6, 6);
            foreach (Coord wall in new[] { new Coord(4, 5), new Coord(5, 4), new Coord(4, 4) })
                grid.SetWalkable(grid.Index(wall), false);

            SearchResult result = new BidirectionalSearch()
                .Search(grid, grid.Index(new Coord(0, 0)), grid.Index(new Coord(5, 5)), new SearchOptions());

            Assert.IsFalse(result.Found);
        }
    }
}
