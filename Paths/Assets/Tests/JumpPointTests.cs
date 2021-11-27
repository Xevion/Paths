using System.Collections.Generic;
using Algorithms;
using NUnit.Framework;
using UnityEngine;

namespace Paths.Tests {
    public class JumpPointTests {
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

        // run an IPathfinding and read the goal's g (path cost), or -1 if it found no path
        private static float Cost(IPathfinding algo, Vector2Int start, Vector2Int end, NodeGrid grid) {
            Stack<Node> path = algo.FindPath(start, end);
            return path == null ? -1f : grid.GetNode(end).Cost.Value;
        }

        // trusted reference: plain Dijkstra over the same 8-connected, no-corner-cutting model JPS uses.
        // proper relaxation (unlike the app's discover-once searches), so this is the true optimum.
        private static float ReferenceCost(NodeGrid grid, Vector2Int start, Vector2Int end) {
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
                        int nx = cur.x + dx, ny = cur.y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        if (!grid.Grid[nx, ny].Walkable) continue;
                        // no corner cutting: a diagonal needs both shared orthogonals open
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
        public void OpenGridIsAStraightDiagonal() {
            var grid = new NodeGrid(8, 8);
            var jps = new JumpPoint(grid);

            Stack<Node> path = jps.FindPath(new Vector2Int(0, 0), new Vector2Int(7, 7));

            Assert.IsNotNull(path);
            Assert.AreEqual(7f * Sqrt2, grid.GetNode(new Vector2Int(7, 7)).Cost.Value, Eps);
        }

        [Test]
        public void FindsOptimalCostOnRandomGrids() {
            var start = new Vector2Int(0, 0);
            var end = new Vector2Int(11, 9);
            for (int seed = 0; seed < 50; seed++) {
                NodeGrid grid = Seeded(seed, 12, 10, 0.28);
                float reference = ReferenceCost(grid, start, end);

                var jps = new JumpPoint(grid);
                float cost = Cost(jps, start, end, grid);
                jps.Cleanup();

                Assert.AreEqual(reference < 0, cost < 0, $"reachability mismatch on seed {seed}");
                if (reference >= 0)
                    Assert.AreEqual(reference, cost, Eps, $"cost mismatch on seed {seed}");
            }
        }
    }
}
