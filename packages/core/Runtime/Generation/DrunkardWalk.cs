#nullable enable
using System;
using Paths.Core.Search;

namespace Paths.Core.Generation {
    /// <summary>
    /// Staggers a walker around the grid, flipping whatever it stands on. Carving out of a solid
    /// grid it leaves one winding cave, which is connected by construction since the walker never
    /// teleports.
    /// </summary>
    public sealed class DrunkardWalk : ILevelGenerator {
        private static readonly Coord[] Directions = {
            new Coord(0, 1), new Coord(0, -1), new Coord(-1, 0), new Coord(1, 0)
        };

        private readonly int _walks;
        private readonly int _steps;
        private readonly bool _add;
        private readonly double _centerBias;
        private readonly double _continueBias;

        /// <param name="walks">How many separate walkers to run.</param>
        /// <param name="steps">How far each one travels.</param>
        /// <param name="add">Place walls when true, clear them when false.</param>
        /// <param name="centerBias">Chance a turn aims back toward the middle of the grid.</param>
        /// <param name="continueBias">Chance of holding the current direction instead of turning.</param>
        public DrunkardWalk(int walks, int steps, bool add, double centerBias = 0.1, double continueBias = 0.2) {
            _walks = walks;
            _steps = steps;
            _add = add;
            _centerBias = centerBias;
            _continueBias = continueBias;
        }

        public void Generate(Grid grid, Random random) {
            var center = new Coord(grid.Width / 2, grid.Height / 2);

            for (int walk = 0; walk < _walks; walk++) {
                Coord at = grid.At(random.Next(grid.CellCount));
                Coord heading = Directions[random.Next(Directions.Length)];

                for (int step = 0; step < _steps; step++) {
                    grid.SetWalkable(grid.Index(at), !_add);

                    // mostly turn, but hold the line often enough to carve corridors rather than blobs
                    if (!Roll(random, _continueBias))
                        heading = Roll(random, _centerBias)
                            ? Toward(at, center, random)
                            : Directions[random.Next(Directions.Length)];

                    Coord next = at + heading;
                    if (!grid.InBounds(next)) {
                        // walked into the edge, so turn rather than stall against it
                        heading = Directions[random.Next(Directions.Length)];
                        continue;
                    }

                    at = next;
                }
            }
        }

        private static bool Roll(Random random, double bias) => random.NextDouble() < bias;

        // one step along whichever axis is furthest from the target
        private static Coord Toward(Coord from, Coord to, Random random) {
            Coord delta = to - from;
            if (delta.X == 0 && delta.Y == 0)
                return Directions[random.Next(Directions.Length)];
            if (Math.Abs(delta.X) >= Math.Abs(delta.Y))
                return delta.X > 0 ? new Coord(1, 0) : new Coord(-1, 0);
            return delta.Y > 0 ? new Coord(0, 1) : new Coord(0, -1);
        }
    }
}
