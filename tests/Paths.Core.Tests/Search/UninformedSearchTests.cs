using NUnit.Framework;
using Paths.Core.Search;
using static Paths.Tests.Search.SearchCorpus;

namespace Paths.Tests.Search {
    public class UninformedSearchTests {
        // with every step costing the same, fewest steps is also cheapest, so breadth-first lands
        // exactly on the reference optimum
        [Test]
        public void BreadthFirstIsOptimalOnUniformSteps() {
            foreach (int size in new[] { 10, 16 })
                foreach (double density in new[] { 0.22, 0.30 })
                    for (int seed = 0; seed < 60; seed++) {
                        Grid grid = Seeded(seed, size, size, density);
                        int start = grid.Index(new Coord(0, 0));
                        int goal = grid.Index(new Coord(size - 1, size - 1));
                        float reference = ReferenceCost(grid, start, goal, false);

                        SearchResult result = new BreadthFirstSearch()
                            .Search(grid, start, goal, new SearchOptions());

                        string where = $"size {size}, density {density}, seed {seed}{Render(grid, start, goal)}";
                        Assert.AreEqual(reference >= 0, result.Found, $"reachability mismatch on {where}");
                        if (reference >= 0)
                            Assert.AreEqual(reference, result.Cost, Eps, $"cost mismatch on {where}");
                        AssertPathIsWalkable(grid, result, start, goal, false, where);
                    }
        }

        /// <summary>
        /// Both are complete, so they agree on whether a path exists even where they disagree on
        /// which one. Depth-first in particular wanders, and the path it comes back with still has
        /// to be a real one.
        /// </summary>
        [Test]
        public void DepthFirstFindsAWalkablePathWhereverOneExists() {
            foreach (int size in new[] { 10, 16 })
                foreach (double density in new[] { 0.22, 0.30 })
                    for (int seed = 0; seed < 60; seed++) {
                        Grid grid = Seeded(seed, size, size, density);
                        int start = grid.Index(new Coord(0, 0));
                        int goal = grid.Index(new Coord(size - 1, size - 1));
                        float reference = ReferenceCost(grid, start, goal, true);

                        SearchResult result = new DepthFirstSearch()
                            .Search(grid, start, goal, new SearchOptions(true));

                        string where = $"size {size}, density {density}, seed {seed}{Render(grid, start, goal)}";
                        Assert.AreEqual(reference >= 0, result.Found, $"reachability mismatch on {where}");
                        AssertPathIsWalkable(grid, result, start, goal, true, where);
                    }
        }

        [Test]
        public void BreadthFirstExpandsInRings() {
            var grid = new Grid(9, 9);
            int start = grid.Index(new Coord(4, 4));
            int goal = grid.Index(new Coord(0, 0));

            SearchResult result = new BreadthFirstSearch().Search(grid, start, goal, new SearchOptions());

            // cells nearer the start have to come out before cells further away
            float previous = -1f;
            foreach (Visit visit in result.Visits) {
                if (visit.Kind != VisitKind.Expanded) continue;
                float steps = Heuristics.Estimate(Heuristic.Manhattan, grid.At(start), grid.At(visit.Cell));
                Assert.GreaterOrEqual(steps, previous - Eps, "expanded a further cell before a nearer one");
                previous = steps;
            }
        }

        // depth-first commits to a direction, so on an open grid it should reach the goal having
        // touched far fewer cells than breadth-first sweeping outward
        [Test]
        public void DepthFirstExpandsFewerCellsThanBreadthFirstOnAnOpenGrid() {
            var grid = new Grid(20, 20);
            int start = grid.Index(new Coord(0, 0));
            int goal = grid.Index(new Coord(19, 19));
            var options = new SearchOptions();

            SearchResult depth = new DepthFirstSearch().Search(grid, start, goal, options);
            SearchResult breadth = new BreadthFirstSearch().Search(grid, start, goal, options);

            Assert.IsTrue(depth.Found);
            Assert.IsTrue(breadth.Found);
            Assert.Less(depth.Expanded, breadth.Expanded);
        }
    }
}
