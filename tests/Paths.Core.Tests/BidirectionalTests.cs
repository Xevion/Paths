using System.Collections.Generic;
using Algorithms;
using NUnit.Framework;
using UnityEngine;
using static Paths.Tests.GridCorpus;

namespace Paths.Tests {
    public class BidirectionalTests {
        [Test]
        public void OpenGridCostIsManhattan() {
            var grid = new NodeGrid(8, 8);
            var bd = new Bidirectional(grid);

            Stack<Node> path = bd.FindPath(new Vector2Int(0, 0), new Vector2Int(7, 7));

            Assert.IsNotNull(path);
            Assert.AreEqual(14f, grid.GetNode(new Vector2Int(7, 7)).Cost.Value, Eps);
        }

        // the real confidence: exact optimal cost + reachability against the reference over a pile of
        // seeded grids, both connectivities, a couple of sizes and densities.
        [Test]
        public void FindsOptimalCostOnRandomGrids() {
            int[] sizes = { 10, 16 };
            double[] densities = { 0.22, 0.30 };
            foreach (bool diagonal in new[] { false, true })
                foreach (int size in sizes)
                    foreach (double density in densities)
                        for (int seed = 0; seed < 60; seed++) {
                            var start = new Vector2Int(0, 0);
                            var end = new Vector2Int(size - 1, size - 1);
                            NodeGrid grid = Seeded(seed, size, size, density);
                            float reference = ReferenceCost(grid, start, end, diagonal);

                            var bd = new Bidirectional(grid, diagonal);
                            float cost = Cost(bd, start, end, grid);
                            bd.Cleanup();

                            string where = $"diagonal {diagonal}, size {size}, density {density}, seed {seed}{Render(grid, start, end)}";
                            Assert.AreEqual(reference < 0, cost < 0, $"reachability mismatch on {where}");
                            if (reference >= 0)
                                Assert.AreEqual(reference, cost, Eps, $"cost mismatch on {where}");
                        }
        }
    }
}
