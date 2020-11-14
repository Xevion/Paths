using Algorithms;

public interface ILevelGenerator {
    /// <summary>
    /// Applies the LevelGenerator's algorithm.
    /// A empty NodeGrid is recommended, as the algorithm may perform in unexpected ways, possibly even fail.
    /// </summary>
    /// <param name="nodeGrid">A NodeGrid object.</param>
    /// <returns>A modified NodeGrid object.</returns>
    NodeGrid Generate(NodeGrid nodeGrid);
}