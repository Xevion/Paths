using System.Collections.Generic;
using System.Text;
using Algorithms;
using UnityEngine;

namespace Paths.Tests {
    /// <summary>
    /// Shared setup for the seeded-grid batteries. Every algorithm gets held against the same
    /// reference optimum over the same grids, so the grid builder and the reference live here
    /// instead of being copy-pasted into each test class.
    /// </summary>
    internal static class GridCorpus {
        public const float Sqrt2 = 1.41421356f;
        public const float Eps = 1e-3f;

        // a seeded scatter of walls, with the start and end corners forced open
        public static NodeGrid Seeded(int seed, int w, int h, double wallChance) {
            var rng = new System.Random(seed);
            var grid = new NodeGrid(w, h);
            foreach (Node node in grid.Iterator())
                if (rng.NextDouble() < wallChance)
                    node.Walkable = false;
            grid.Grid[0, 0].Walkable = true;
            grid.Grid[w - 1, h - 1].Walkable = true;
            return grid;
        }

        // run a search and read the goal's g, or -1 if it found no path
        public static float Cost(IPathfinding algo, Vector2Int start, Vector2Int end, NodeGrid grid) {
            Stack<Node> path = algo.FindPath(start, end);
            return path == null ? -1f : grid.GetNode(end).Cost.Value;
        }

        /// <summary>
        /// Draws the grid into a failure message. A seed and a pair of floats say nothing about why
        /// a search went wrong; the map it was walking usually says it immediately.
        /// </summary>
        public static string Render(NodeGrid grid, Vector2Int start, Vector2Int end) {
            var sb = new StringBuilder();
            sb.AppendLine();
            // top row first so the output reads the way the grid is drawn on screen
            for (int y = grid.Height - 1; y >= 0; y--) {
                sb.Append("    ");
                for (int x = 0; x < grid.Width; x++) {
                    var at = new Vector2Int(x, y);
                    if (at == start) sb.Append('S');
                    else if (at == end) sb.Append('E');
                    else sb.Append(grid.Grid[x, y].Walkable ? '.' : '#');
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// Plain Dijkstra with proper relaxation over the same 4- or 8-connected no-corner-cutting
        /// model the app uses. The app's own searches score a node once at discovery and never
        /// revisit it, so they can't be trusted as an optimum. This can.
        /// </summary>
        public static float ReferenceCost(NodeGrid grid, Vector2Int start, Vector2Int end, bool diagonal = true) {
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
    }
}
