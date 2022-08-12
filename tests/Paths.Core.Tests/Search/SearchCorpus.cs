using System.Collections.Generic;
using System.Text;
using Paths.Core.Search;

namespace Paths.Tests.Search {
    /// <summary>
    /// Shared grids and the trusted answer to hold the searches against.
    /// </summary>
    internal static class SearchCorpus {
        public const float Eps = 1e-3f;

        public static Grid Seeded(int seed, int width, int height, double wallChance) {
            var rng = new System.Random(seed);
            var grid = new Grid(width, height);
            for (int i = 0; i < grid.CellCount; i++)
                if (rng.NextDouble() < wallChance)
                    grid.SetWalkable(i, false);
            grid.SetWalkable(grid.Index(new Coord(0, 0)), true);
            grid.SetWalkable(grid.Index(new Coord(width - 1, height - 1)), true);
            return grid;
        }

        /// <summary>
        /// Dijkstra written the slow obvious way: scan the open set for the cheapest cell, relax its
        /// neighbours, repeat. Deliberately shares nothing with the real implementation, so a bug in
        /// the heap or the expand loop cannot hide inside the answer being compared against.
        /// </summary>
        public static float ReferenceCost(Grid grid, int start, int goal, bool diagonal) {
            var dist = new float[grid.CellCount];
            for (int i = 0; i < dist.Length; i++)
                dist[i] = float.PositiveInfinity;
            dist[start] = 0f;

            var open = new List<int> { start };
            var settled = new bool[grid.CellCount];

            while (open.Count > 0) {
                int best = 0;
                for (int i = 1; i < open.Count; i++)
                    if (dist[open[i]] < dist[open[best]])
                        best = i;

                int current = open[best];
                open.RemoveAt(best);
                if (settled[current])
                    continue;
                settled[current] = true;
                if (current == goal)
                    return dist[goal];

                Coord at = grid.At(current);
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++) {
                        if (dx == 0 && dy == 0) continue;
                        if (!diagonal && dx != 0 && dy != 0) continue;

                        var to = new Coord(at.X + dx, at.Y + dy);
                        if (!grid.InBounds(to)) continue;
                        int next = grid.Index(to);
                        if (!grid.IsWalkable(next)) continue;

                        // no corner cutting: both orthogonals beside the diagonal have to be open
                        if (dx != 0 && dy != 0 &&
                            (!grid.IsWalkable(grid.Index(new Coord(at.X + dx, at.Y))) ||
                             !grid.IsWalkable(grid.Index(new Coord(at.X, at.Y + dy)))))
                            continue;

                        float step = (dx != 0 && dy != 0 ? Grid.DiagonalCost : 1f) * grid.Weight(next);
                        if (dist[current] + step + Eps < dist[next]) {
                            dist[next] = dist[current] + step;
                            open.Add(next);
                        }
                    }
            }

            return float.IsPositiveInfinity(dist[goal]) ? -1f : dist[goal];
        }

        public static string Render(Grid grid, int start, int goal) {
            var sb = new StringBuilder();
            sb.AppendLine();
            for (int y = grid.Height - 1; y >= 0; y--) {
                sb.Append("    ");
                for (int x = 0; x < grid.Width; x++) {
                    int index = grid.Index(new Coord(x, y));
                    if (index == start) sb.Append('S');
                    else if (index == goal) sb.Append('E');
                    else sb.Append(grid.IsWalkable(index) ? '.' : '#');
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>Every step contiguous, every cell walkable, and the ends where they should be.</summary>
        public static void AssertPathIsWalkable(Grid grid, SearchResult result, int start, int goal, bool diagonal, string where) {
            if (!result.Found)
                return;

            NUnit.Framework.Assert.AreEqual(start, result.Path[0], $"path should start at the start on {where}");
            NUnit.Framework.Assert.AreEqual(goal, result.Path[result.Path.Count - 1], $"path should end at the goal on {where}");

            for (int i = 0; i < result.Path.Count; i++) {
                NUnit.Framework.Assert.IsTrue(grid.IsWalkable(result.Path[i]), $"path runs through a wall on {where}");
                if (i == 0) continue;

                Coord a = grid.At(result.Path[i - 1]);
                Coord b = grid.At(result.Path[i]);
                int dx = System.Math.Abs(a.X - b.X);
                int dy = System.Math.Abs(a.Y - b.Y);
                NUnit.Framework.Assert.IsTrue(dx <= 1 && dy <= 1 && dx + dy > 0, $"path jumps between cells on {where}");
                if (!diagonal)
                    NUnit.Framework.Assert.IsTrue(dx + dy == 1, $"path moved diagonally with diagonals off on {where}");
            }
        }
    }
}
