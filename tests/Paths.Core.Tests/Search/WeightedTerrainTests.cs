using NUnit.Framework;
using Paths.Core.Search;
using static Paths.Tests.Search.SearchCorpus;

namespace Paths.Tests.Search {
    /// <summary>
    /// Terrain that costs less than a plain cell. A heuristic counts cells and assumes each is worth
    /// at least one, so a grid holding anything cheaper leaves A* overestimating what is left to walk
    /// and settling for a route it should have looked past.
    /// </summary>
    public class WeightedTerrainTests {
        // road, plain, mud, water
        private static readonly float[] Tiers = { 0.5f, 1f, 2f, 4f };

        [TestCase(Heuristic.Manhattan, false)]
        [TestCase(Heuristic.Euclidean, false)]
        [TestCase(Heuristic.Euclidean, true)]
        [TestCase(Heuristic.Octile, true)]
        [TestCase(Heuristic.Chebyshev, true)]
        public void AStarStaysOptimalOverCheapTerrain(Heuristic heuristic, bool diagonal) {
            foreach (int size in new[] { 10, 16 })
                for (int seed = 0; seed < 60; seed++) {
                    Grid grid = Weighted(seed, size, size, 0.20, Tiers);
                    int start = grid.Index(new Coord(0, 0));
                    int goal = grid.Index(new Coord(size - 1, size - 1));
                    float reference = ReferenceCost(grid, start, goal, diagonal);

                    SearchResult result = new AStarSearch()
                        .Search(grid, start, goal, new SearchOptions(diagonal, heuristic));

                    string where = $"{heuristic}, diagonal {diagonal}, size {size}, seed {seed}{Render(grid, start, goal)}";
                    Assert.AreEqual(reference >= 0, result.Found, $"reachability mismatch on {where}");
                    if (reference >= 0)
                        Assert.AreEqual(reference, result.Cost, Eps, $"cost mismatch on {where}");
                    AssertPathIsWalkable(grid, result, start, goal, diagonal, where);
                }
        }

        /// <summary>Cheap terrain is worth a detour, and the cost has to say so.</summary>
        [Test]
        public void ARoadIsWorthTheDetour() {
            var grid = new Grid(5, 3);
            // a road along the bottom row, so going down and along beats cutting straight across
            for (int x = 0; x < 5; x++)
                grid.SetWeight(grid.Index(new Coord(x, 0)), 0.25f);
            for (int x = 1; x < 4; x++)
                grid.SetWeight(grid.Index(new Coord(x, 1)), 3f);

            int start = grid.Index(new Coord(0, 1));
            int goal = grid.Index(new Coord(4, 1));

            SearchResult result = new AStarSearch().Search(grid, start, goal, new SearchOptions());

            float reference = ReferenceCost(grid, start, goal, false);
            Assert.AreEqual(reference, result.Cost, Eps);
            CollectionAssert.Contains(result.Path, grid.Index(new Coord(2, 0)), "should have taken the road");
        }

        [Test]
        public void MinWeightTracksTheCheapestCell() {
            var grid = new Grid(4, 4);
            Assert.AreEqual(1f, grid.MinWeight, Eps);

            grid.SetWeight(grid.Index(new Coord(1, 1)), 0.4f);
            Assert.AreEqual(0.4f, grid.MinWeight, Eps);

            // the only cheap cell going back to normal has to lift the minimum with it
            grid.SetWeight(grid.Index(new Coord(1, 1)), 1f);
            Assert.AreEqual(1f, grid.MinWeight, Eps);

            grid.SetWeight(grid.Index(new Coord(2, 2)), 5f);
            Assert.AreEqual(1f, grid.MinWeight, Eps);
        }

        [Test]
        public void DijkstraIsUnaffectedByTerrainScaling() {
            for (int seed = 0; seed < 30; seed++) {
                Grid grid = Weighted(seed, 12, 12, 0.20, Tiers);
                int start = grid.Index(new Coord(0, 0));
                int goal = grid.Index(new Coord(11, 11));
                float reference = ReferenceCost(grid, start, goal, true);

                SearchResult result = new DijkstraSearch().Search(grid, start, goal, new SearchOptions(true));

                string where = $"seed {seed}{Render(grid, start, goal)}";
                Assert.AreEqual(reference >= 0, result.Found, $"reachability mismatch on {where}");
                if (reference >= 0)
                    Assert.AreEqual(reference, result.Cost, Eps, $"cost mismatch on {where}");
            }
        }
    }
}
