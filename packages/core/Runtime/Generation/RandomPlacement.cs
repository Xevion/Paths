#nullable enable
using System;
using Paths.Core.Search;

namespace Paths.Core.Generation {
    /// <summary>
    /// Scatters walls with no pattern at all, up to a target share of the grid.
    /// </summary>
    public sealed class RandomPlacement : ILevelGenerator {
        private readonly float _density;
        private readonly bool _fillTo;
        private readonly bool _add;

        /// <param name="density">Share of the grid to end up covered.</param>
        /// <param name="fillTo">Treat density as the total to reach, rather than an amount to add.</param>
        /// <param name="add">Place walls when true, clear them when false.</param>
        public RandomPlacement(float density, bool fillTo, bool add) {
            _density = density;
            _fillTo = fillTo;
            _add = add;
        }

        public void Generate(Grid grid, Random random) {
            int remaining = (int) Math.Round(grid.CellCount * _density);
            if (_fillTo)
                remaining -= Count(grid, !_add);

            // never ask for more cells than there are left to flip, or the loop below spins forever
            // hunting for candidates that do not exist
            remaining = Math.Max(0, Math.Min(remaining, Count(grid, _add)));

            while (remaining > 0) {
                int cell = random.Next(grid.CellCount);
                if (grid.IsWalkable(cell) != _add)
                    continue;
                grid.SetWalkable(cell, !_add);
                remaining--;
            }
        }

        private static int Count(Grid grid, bool walkable) {
            int found = 0;
            for (int i = 0; i < grid.CellCount; i++)
                if (grid.IsWalkable(i) == walkable)
                    found++;
            return found;
        }
    }
}
