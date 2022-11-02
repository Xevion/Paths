#nullable enable
using System.Collections.Generic;
using Paths.Core.Search;

namespace Paths.Core {
    /// <summary>
    /// Which neighbours each cell steps to along the route, one bit per direction. Grid adjacency
    /// is not route adjacency: a route folding past itself leaves two of its cells side by side
    /// with no step between them, and only the order can tell the difference.
    /// </summary>
    public static class PathLinks {
        /// <summary>
        /// The bit for a step to the neighbour at (dx, dy), 0 for anything else. Row-major over the
        /// 3x3 with the centre dropped, the order a renderer walks its own neighbourhood in.
        /// </summary>
        public static int Bit(int dx, int dy) {
            if (dx < -1 || dx > 1 || dy < -1 || dy > 1 || (dx == 0 && dy == 0))
                return 0;
            int index = (dy + 1) * 3 + (dx + 1);
            return 1 << (index > 4 ? index - 1 : index);
        }

        /// <summary>Whether a mask joins its cell to the neighbour at (dx, dy).</summary>
        public static bool Linked(byte mask, int dx, int dy) {
            int bit = Bit(dx, dy);
            return bit != 0 && (mask & bit) != 0;
        }

        /// <summary>
        /// The mask for every cell of a route given start first, marked at both ends of each step.
        /// All zero when nothing was found, so a stale route cannot be left drawn.
        /// </summary>
        public static byte[,] Of(Grid grid, IReadOnlyList<int> path) {
            var links = new byte[grid.Width, grid.Height];
            for (int i = 1; i < path.Count; i++) {
                Coord a = grid.At(path[i - 1]);
                Coord b = grid.At(path[i]);
                int forward = Bit(b.X - a.X, b.Y - a.Y);
                // contiguity is held to where the searches are tested; a gap beats an exception
                if (forward == 0)
                    continue;
                links[a.X, a.Y] |= (byte) forward;
                links[b.X, b.Y] |= (byte) Bit(a.X - b.X, a.Y - b.Y);
            }
            return links;
        }
    }
}
