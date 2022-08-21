using System;
using Paths.Core.Search;

namespace Paths.Benchmarks {
    internal static class Grids {
        /// <summary>
        /// A seeded wall scatter that is actually solvable. The scatter can wall a corner in on
        /// itself or cut the grid in half, and a search that gives up early measures nothing, so
        /// walk the seeds in order until one produces a grid with a path. Deterministic either way.
        /// </summary>
        public static Grid Seeded(int size, double wallChance, int seed = 7) {
            for (int attempt = 0; attempt < 64; attempt++) {
                Grid grid = Scatter(size, wallChance, seed + attempt);
                if (HasPath(grid, size))
                    return grid;
            }
            throw new InvalidOperationException(
                $"no solvable {size}x{size} grid at {wallChance:P0} walls within 64 seeds of {seed}");
        }

        private static Grid Scatter(int size, double wallChance, int seed) {
            var rng = new Random(seed);
            var grid = new Grid(size, size);
            for (int i = 0; i < grid.CellCount; i++)
                if (rng.NextDouble() < wallChance)
                    grid.SetWalkable(i, false);
            Open(grid, new Coord(0, 0));
            Open(grid, new Coord(size - 1, size - 1));
            return grid;
        }

        private static void Open(Grid grid, Coord at) {
            for (int x = at.X - 1; x <= at.X + 1; x++)
                for (int y = at.Y - 1; y <= at.Y + 1; y++) {
                    var cell = new Coord(x, y);
                    if (grid.InBounds(cell))
                        grid.SetWalkable(grid.Index(cell), true);
                }
        }

        private static bool HasPath(Grid grid, int size) =>
            new DijkstraSearch()
                .Search(grid, grid.Index(new Coord(0, 0)), grid.Index(new Coord(size - 1, size - 1)),
                    new SearchOptions(true))
                .Found;
    }
}
