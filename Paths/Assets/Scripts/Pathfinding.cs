using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// A general interface for implementing pathfinding algorithms.
/// </summary>
public interface Pathfinding {
    /// <summary>
    /// Finds a valid path between two Node objects.
    /// </summary>
    /// <param name="Start"></param>
    /// <param name="End"></param>
    /// <returns>A List of Grid objects representing the timeline of Pathfinding</returns>
    Tuple<List<Grid>, Stack<Node>> FindPath(Vector2 Start, Vector2 End);
}