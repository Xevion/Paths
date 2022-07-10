using System.Collections.Generic;
using Algorithms;
using NUnit.Framework;
using UnityEngine;
using static Paths.Tests.GridCorpus;

namespace Paths.Tests {
    public class JumpPointTests {
        [Test]
        public void OpenGridIsAStraightDiagonal() {
            var grid = new NodeGrid(8, 8);
            var jps = new JumpPoint(grid);

            Stack<Node> path = jps.FindPath(new Vector2Int(0, 0), new Vector2Int(7, 7));

            Assert.IsNotNull(path);
            Assert.AreEqual(7f * Sqrt2, grid.GetNode(new Vector2Int(7, 7)).Cost.Value, Eps);
        }

        // the real confidence: against a trusted optimum on a pile of seeded grids, a couple of sizes
        // and wall densities, JPS has to agree on both reachability and exact cost every time.
        [Test]
        public void FindsOptimalCostOnRandomGrids() {
            int[] sizes = { 10, 16 };
            double[] densities = { 0.22, 0.30 };
            foreach (int size in sizes)
                foreach (double density in densities)
                    for (int seed = 0; seed < 60; seed++) {
                        var start = new Vector2Int(0, 0);
                        var end = new Vector2Int(size - 1, size - 1);
                        NodeGrid grid = Seeded(seed, size, size, density);
                        float reference = ReferenceCost(grid, start, end);

                        var jps = new JumpPoint(grid);
                        float cost = Cost(jps, start, end, grid);
                        jps.Cleanup();

                        string where = $"size {size}, density {density}, seed {seed}";
                        Assert.AreEqual(reference < 0, cost < 0, $"reachability mismatch on {where}");
                        if (reference >= 0)
                            Assert.AreEqual(reference, cost, Eps, $"cost mismatch on {where}");
                    }
        }
    }
}
