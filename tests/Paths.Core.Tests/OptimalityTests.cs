using Algorithms;
using NUnit.Framework;
using UnityEngine;
using static Paths.Tests.GridCorpus;

namespace Paths.Tests {
    public class OptimalityTests {
        /// <summary>
        /// Every search here orders by an admissible heuristic, so each one has to land exactly on the
        /// reference optimum. Dijkstra passed this even before nodes could be relaxed, because ordering
        /// by g means the first route found is already the cheapest. A* orders by f, where g arrives
        /// out of order, so it is the one that actually exercises relaxation.
        ///
        /// Manhattan is missing from the diagonal cases on purpose: it scores a diagonal step as 2
        /// against an actual cost of 1.41, which overestimates and is not admissible there.
        /// </summary>
        [TestCase(Algorithm.Dijkstra, Heuristic.Manhattan, false)]
        [TestCase(Algorithm.Dijkstra, Heuristic.Manhattan, true)]
        [TestCase(Algorithm.AStar, Heuristic.Manhattan, false)]
        [TestCase(Algorithm.AStar, Heuristic.Euclidean, true)]
        [TestCase(Algorithm.AStar, Heuristic.Chebyshev, true)]
        public void MatchesTheReferenceOptimum(Algorithm algorithm, Heuristic heuristic, bool diagonal) {
            int[] sizes = { 10, 16 };
            double[] densities = { 0.22, 0.30 };
            foreach (int size in sizes)
                foreach (double density in densities)
                    for (int seed = 0; seed < 60; seed++) {
                        var start = new Vector2Int(0, 0);
                        var end = new Vector2Int(size - 1, size - 1);
                        NodeGrid grid = Seeded(seed, size, size, density);
                        float reference = ReferenceCost(grid, start, end, diagonal);

                        IPathfinding search = algorithm == Algorithm.Dijkstra
                            ? new Dijkstra(grid, diagonal)
                            : (IPathfinding) new AStar(grid, heuristic, diagonal);
                        float cost = Cost(search, start, end, grid);

                        string where = $"{algorithm}/{heuristic}, diagonal {diagonal}, size {size}, " +
                                       $"density {density}, seed {seed}{Render(grid, start, end)}";
                        Assert.AreEqual(reference < 0, cost < 0, $"reachability mismatch on {where}");
                        if (reference >= 0)
                            Assert.AreEqual(reference, cost, Eps, $"cost mismatch on {where}");
                    }
        }
    }
}
