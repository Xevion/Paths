#nullable enable
using System;

namespace Paths.Core.Search {
    public enum Heuristic {
        Manhattan,
        Euclidean,
        Chebyshev,
        Octile
    }

    public static class Heuristics {
        /// <summary>
        /// Estimated cost from one cell to another. A heuristic that never overestimates keeps A*
        /// on the shortest path; one that overestimates trades that away for speed.
        ///
        /// Manhattan fits 4-direction movement. It charges 2 for a diagonal that really costs 1.41,
        /// so it overestimates as soon as diagonals are allowed. Octile is the one that fits
        /// 8-direction movement exactly.
        /// </summary>
        public static float Estimate(Heuristic kind, Coord from, Coord to) {
            int dx = Math.Abs(from.X - to.X);
            int dy = Math.Abs(from.Y - to.Y);

            switch (kind) {
                case Heuristic.Manhattan:
                    return dx + dy;
                case Heuristic.Euclidean:
                    return (float) Math.Sqrt(dx * dx + dy * dy);
                case Heuristic.Chebyshev:
                    return Math.Max(dx, dy);
                case Heuristic.Octile:
                    // straight for the part that has to be walked square-on, diagonal for the rest
                    return dx + dy + (Grid.DiagonalCost - 2f) * Math.Min(dx, dy);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "unknown heuristic");
            }
        }
    }
}
