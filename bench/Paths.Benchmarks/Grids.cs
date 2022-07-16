using System;
using System.Collections.Generic;
using Algorithms;
using UnityEngine;

namespace Paths.Benchmarks {
    internal static class Grids {
        /// <summary>
        /// A seeded wall scatter that is actually solvable. The scatter can wall a corner in on
        /// itself, or cut the grid in half, and a search that gives up early measures nothing, so
        /// walk the seeds in order until one produces a grid with a path. Deterministic either way.
        /// </summary>
        public static NodeGrid Seeded(int size, double wallChance, int seed = 7) {
            for (int attempt = 0; attempt < 64; attempt++) {
                NodeGrid grid = Scatter(size, wallChance, seed + attempt);
                if (HasPath(grid, size))
                    return grid;
            }
            throw new InvalidOperationException(
                $"no solvable {size}x{size} grid at {wallChance:P0} walls within 64 seeds of {seed}");
        }

        private static NodeGrid Scatter(int size, double wallChance, int seed) {
            var rng = new System.Random(seed);
            var grid = new NodeGrid(size, size, new System.Random(seed));
            foreach (Node node in grid.Iterator())
                if (rng.NextDouble() < wallChance)
                    node.Walkable = false;
            Open(grid, new Vector2Int(0, 0));
            Open(grid, new Vector2Int(size - 1, size - 1));
            return grid;
        }

        private static void Open(NodeGrid grid, Vector2Int at) {
            for (int x = at.x - 1; x <= at.x + 1; x++)
                for (int y = at.y - 1; y <= at.y + 1; y++) {
                    var cell = new Vector2Int(x, y);
                    if (grid.IsValid(cell))
                        grid.GetNode(cell).Walkable = true;
                }
        }

        private static bool HasPath(NodeGrid grid, int size) {
            var search = new Dijkstra(grid, true);
            Stack<Node> path = search.FindPath(new Vector2Int(0, 0), new Vector2Int(size - 1, size - 1));
            search.Cleanup();
            return path != null;
        }
    }
}
