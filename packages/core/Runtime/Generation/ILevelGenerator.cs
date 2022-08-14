#nullable enable
using System;
using Paths.Core.Search;

namespace Paths.Core.Generation {
    /// <summary>
    /// Paints a starting layout onto a grid, in place. Additive generators expect an open grid to
    /// place walls on; subtractive ones expect a solid grid to carve out of.
    ///
    /// The random source is handed in rather than held, which keeps the grid plain data and lets a
    /// caller replay the same layout by replaying the same seed.
    /// </summary>
    public interface ILevelGenerator {
        void Generate(Grid grid, Random random);
    }
}
