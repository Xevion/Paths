using System.Collections.Generic;
using Algorithms;
using NUnit.Framework;
using UnityEngine;

namespace Paths.Tests {
    public class BidirectionalTests {
        private const float Sqrt2 = 1.41421356f;
        private const float Eps = 1e-3f;

        // a grid with a seeded scatter of walls, start/end corners forced open
        private static NodeGrid Seeded(int seed, int w, int h, double wallChance) {
            var rng = new System.Random(seed);
            var grid = new NodeGrid(w, h);
            foreach (Node node in grid.Iterator())
                if (rng.NextDouble() < wallChance)
                    node.Walkable = false;
            grid.Grid[0, 0].Walkable = true;
            grid.Grid[w - 1, h - 1].Walkable = true;
            return grid;
        }

        // run the search and read the goal's g (path cost), or -1 if it found no path
        private static float Cost(IPathfinding algo, Vector2Int start, Vector2Int end, NodeGrid grid) {
            Stack<Node> path = algo.FindPath(start, end);
            return path == null ? -1f : grid.GetNode(end).Cost.Value;
        }

        // trusted reference: plain relaxing Dijkstra over the same 4- or 8-connected (no corner cutting)
        // model, the true optimum to hold the bidirectional search against.
        private static float ReferenceCost(NodeGrid grid, Vector2Int start, Vector2Int end, bool diagonal) {
            int w = grid.Width, h = grid.Height;
            var dist = new float[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    dist[x, y] = float.PositiveInfinity;
            dist[start.x, start.y] = 0f;

            var open = new List<Vector2Int> { start };
            while (open.Count > 0) {
                int best = 0;
                for (int i = 1; i < open.Count; i++)
                    if (dist[open[i].x, open[i].y] < dist[open[best].x, open[best].y])
                        best = i;
                Vector2Int cur = open[best];
                open.RemoveAt(best);
                if (cur == end)
                    return dist[end.x, end.y];

                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++) {
                        if (dx == 0 && dy == 0) continue;
                        if (!diagonal && dx != 0 && dy != 0) continue;
                        int nx = cur.x + dx, ny = cur.y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        if (!grid.Grid[nx, ny].Walkable) continue;
                        if (dx != 0 && dy != 0 &&
                            (!grid.Grid[cur.x + dx, cur.y].Walkable || !grid.Grid[cur.x, cur.y + dy].Walkable))
                            continue;

                        float step = dx != 0 && dy != 0 ? Sqrt2 : 1f;
                        float nd = dist[cur.x, cur.y] + step;
                        if (nd + Eps < dist[nx, ny]) {
                            dist[nx, ny] = nd;
                            open.Add(new Vector2Int(nx, ny));
                        }
                    }
            }
            return float.IsPositiveInfinity(dist[end.x, end.y]) ? -1f : dist[end.x, end.y];
        }

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

                            string where = $"diagonal {diagonal}, size {size}, density {density}, seed {seed}";
                            Assert.AreEqual(reference < 0, cost < 0, $"reachability mismatch on {where}");
                            if (reference >= 0)
                                Assert.AreEqual(reference, cost, Eps, $"cost mismatch on {where}");
                        }
        }
    }
}
