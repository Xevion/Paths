using Algorithms;

public interface ILevelGenerator {
    /// <summary>
    /// Applies the generator's algorithm to a grid, in place. Additive generators expect an open grid
    /// to place walls on; subtractive ones (rooms, walks) expect a solid one to carve out of.
    /// </summary>
    /// <param name="nodeGrid">The grid to lay out.</param>
    /// <returns>The same grid, modified.</returns>
    NodeGrid Generate(NodeGrid nodeGrid);
}